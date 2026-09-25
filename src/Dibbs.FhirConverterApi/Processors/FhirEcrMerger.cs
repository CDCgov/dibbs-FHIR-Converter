using System.Net;
using Dibbs.FhirConverterApi.Models;
using Hl7.Fhir.Model;

namespace Dibbs.FhirConverterApi.Processors;

/// <summary>
/// Combines separate FHIR eICR and RR document Bundles into the flat eICR Bundle
/// shape consumed by the eCR Viewer.
/// </summary>
internal static class FhirEcrMerger
{
    private const string EicrCompositionProfile =
        "http://hl7.org/fhir/us/ecr/StructureDefinition/eicr-composition";

    private const string RrCompositionProfile =
        "http://hl7.org/fhir/us/ecr/StructureDefinition/rr-composition";

    private const string ProcessingStatusProfile =
        "http://hl7.org/fhir/us/ecr/StructureDefinition/rr-eicr-processing-status-observation";

    private const string ProcessingStatusReasonProfile =
        "http://hl7.org/fhir/us/ecr/StructureDefinition/rr-eicr-processing-status-reason-observation";

    private const string RelevantConditionProfile =
        "http://hl7.org/fhir/us/ecr/StructureDefinition/rr-relevant-reportable-condition-observation";

    private const string ReportabilityInformationProfile =
        "http://hl7.org/fhir/us/ecr/StructureDefinition/rr-reportability-information-observation";

    private const string RrDocumentReferenceProfile =
        "http://hl7.org/fhir/us/ecr/StructureDefinition/rr-documentreference";

    private const string RulesAuthoringAgencyProfile =
        "http://hl7.org/fhir/us/ecr/StructureDefinition/rr-rules-authoring-agency-organization";

    private const string RoutingEntityProfile =
        "http://hl7.org/fhir/us/ecr/StructureDefinition/rr-routing-entity-organization";

    private const string ResponsibleAgencyProfile =
        "http://hl7.org/fhir/us/ecr/StructureDefinition/rr-responsible-agency-organization";

    private const string ProcessingStatusExtension =
        "http://hl7.org/fhir/us/ecr/StructureDefinition/rr-eicr-processing-status-extension";

    private const string InitiationTypeExtension =
        "http://hl7.org/fhir/us/ecr/StructureDefinition/eicr-initiation-type-extension";

    private const string ResourceOwnershipTagSystem =
        "https://github.com/CDCgov/dibbs-FHIR-Converter/CodeSystem/fhir-ecr-merger";

    private const string AddedRrProfileTagSystem =
        "https://github.com/CDCgov/dibbs-FHIR-Converter/CodeSystem/fhir-ecr-merger-added-profile";

    private const string RetainedEicrResourceTagCode = "retained-eicr-resource";

    private const string AppendedRrResourceTagCode = "appended-rr-resource";

    private const string ReportabilityResponseSectionTitle =
        "Reportability Response Information Section";

    private const string ReportabilityResponseSectionId =
        "reportability-response-information";

    private const string ReportabilityResponseCode = "88085-6";

    private static readonly HashSet<string> ViewerRrProfiles = new (StringComparer.Ordinal)
    {
        ProcessingStatusProfile,
        ProcessingStatusReasonProfile,
        RelevantConditionProfile,
        ReportabilityInformationProfile,
        RrDocumentReferenceProfile,
        RulesAuthoringAgencyProfile,
        RoutingEntityProfile,
        ResponsibleAgencyProfile,
    };

    /// <summary>
    /// Normalizes a standalone FHIR Bundle so that all resources have logical IDs
    /// derived from their entry fullUrl, and all internal references are rewritten
    /// to relative ResourceType/id references.
    /// </summary>
    /// <param name="bundle">The input FHIR Bundle.</param>
    /// <returns>The normalized FHIR Bundle.</returns>
    /// <remarks>The supplied Bundle is normalized in place.</remarks>
    public static Bundle Normalize(Bundle bundle)
    {
        EnsureBundleId(bundle);

        var indexedEntries = IndexEntries(bundle.Entry);
        var referenceMap = BuildReferenceMap(indexedEntries);

        RewriteReferences(bundle, referenceMap);

        return bundle;
    }

    /// <summary>
    /// Merges the RR resources used by the Viewer into the eICR document Bundle.
    /// </summary>
    /// <param name="eicrBundle">The eICR document Bundle.</param>
    /// <param name="rrBundle">The RR document Bundle.</param>
    /// <returns>The merged eICR Bundle.</returns>
    /// <remarks>The supplied Bundles are consumed and may be mutated. Callers
    /// should pass newly deserialized instances.</remarks>
    public static Bundle Merge(Bundle eicrBundle, Bundle rrBundle)
    {
        EnsureBundleId(eicrBundle);
        RemoveExistingViewerRrEntries(eicrBundle.Entry);

        var indexedEicrEntries = IndexEntries(eicrBundle.Entry);
        var indexedRrEntries = IndexEntries(rrBundle.Entry);
        var (eicrComposition, eicrPatient) = ValidateDocumentBundle(
            eicrBundle,
            indexedEicrEntries,
            EicrCompositionProfile,
            "FHIR eICR input must contain exactly one eICR Composition and one Patient.");
        var (rrComposition, rrPatient) = ValidateDocumentBundle(
            rrBundle,
            indexedRrEntries,
            RrCompositionProfile,
            "FHIR RR input must contain exactly one RR Composition and one Patient.");

        var eicrIndex = BuildEntryIndex(indexedEicrEntries);
        var rrIndex = BuildEntryIndex(indexedRrEntries);
        var selectedRrEntries = SelectViewerRrEntries(indexedRrEntries, rrIndex);
        var referenceMap = BuildReferenceMap(
            indexedEicrEntries.Concat(selectedRrEntries));

        MapRrPatientToEicrPatient(
            eicrPatient,
            rrPatient,
            eicrIndex,
            referenceMap);
        var rrEntriesToAppend = ReconcileIdentityCollisions(
            selectedRrEntries,
            eicrIndex,
            referenceMap);

        RewriteReferences(eicrBundle, referenceMap);
        RewriteReferences(rrBundle, referenceMap);

        AppendSelectedEntries(eicrBundle.Entry, rrEntriesToAppend);
        ReplaceReportabilityResponseSection(
            (Composition)eicrComposition.Resource,
            (Composition)rrComposition.Resource,
            selectedRrEntries,
            referenceMap);

        return eicrBundle;
    }

    private static void EnsureBundleId(Bundle bundle)
    {
        if (!string.IsNullOrWhiteSpace(bundle.Id))
        {
            return;
        }

        bundle.IdElement ??= new Id();
        bundle.IdElement.Value = Guid.NewGuid().ToString();
    }

    private static List<IndexedEntry> IndexEntries(
        IEnumerable<Bundle.EntryComponent> entries)
    {
        var indexedEntries = new List<IndexedEntry>();

        foreach (var entry in entries)
        {
            if (entry.Resource is not { } resource)
            {
                continue;
            }

            var resourceType = resource.TypeName;
            var resourceId = resource.Id;

            if (string.IsNullOrWhiteSpace(resourceId))
            {
                resourceId = DeriveResourceId(entry.FullUrl);
                resource.IdElement ??= new Id();
                resource.IdElement.Value = resourceId;
            }

            indexedEntries.Add(new IndexedEntry(
                entry,
                resource,
                $"{resourceType}/{resourceId}",
                entry.FullUrl));
        }

        return indexedEntries;
    }

    private static void RemoveExistingViewerRrEntries(
        IList<Bundle.EntryComponent> entries)
    {
        var removeLegacyRrEntries = ContainsReportabilityResponseSection(entries);

        for (var index = entries.Count - 1; index >= 0; index--)
        {
            if (entries[index].Resource is not { } resource)
            {
                continue;
            }

            if (HasRetainedEicrResourceTag(resource))
            {
                RestoreRetainedEicrResource(resource);
                continue;
            }

            if (HasAppendedRrResourceTag(resource) ||
                (removeLegacyRrEntries && HasViewerRrProfile(resource)))
            {
                entries.RemoveAt(index);
            }
        }
    }

    private static bool ContainsReportabilityResponseSection(
        IEnumerable<Bundle.EntryComponent> entries)
    {
        return entries
            .Select(entry => entry.Resource)
            .OfType<Composition>()
            .Any(composition => composition.Section.Any(IsReportabilityResponseSection));
    }

    private static bool IsReportabilityResponseSection(
        Composition.SectionComponent section)
    {
        return section.Title == ReportabilityResponseSectionTitle ||
            HasCode(section.Code, ReportabilityResponseCode);
    }

    private static (IndexedEntry Composition, IndexedEntry Patient)
        ValidateDocumentBundle(
            Bundle bundle,
            IReadOnlyList<IndexedEntry> entries,
            string compositionProfile,
            string errorMessage)
    {
        var compositions = entries.Where(entry =>
            entry.Resource is Composition &&
            HasProfile(entry.Resource, compositionProfile)).ToList();
        var patients = entries.Where(entry => entry.Resource is Patient).ToList();

        if (bundle.Type != Bundle.BundleType.Document ||
            compositions.Count != 1 ||
            patients.Count != 1)
        {
            throw new UserFacingException(
                errorMessage,
                HttpStatusCode.UnprocessableEntity);
        }

        return (compositions[0], patients[0]);
    }

    private static string DeriveResourceId(string? fullUrl)
    {
        const string urnUuidPrefix = "urn:uuid:";
        string? candidate = null;

        if (fullUrl?.StartsWith(urnUuidPrefix, StringComparison.OrdinalIgnoreCase) == true)
        {
            candidate = fullUrl[urnUuidPrefix.Length..];
        }
        else if (Uri.TryCreate(fullUrl, UriKind.Absolute, out var uri))
        {
            candidate = uri.Segments.LastOrDefault()?.Trim('/');
        }

        return candidate is not null && Id.IsValidValue(candidate)
            ? candidate
            : Guid.NewGuid().ToString();
    }

    private static Dictionary<string, IndexedEntry> BuildEntryIndex(
        IEnumerable<IndexedEntry> entries)
    {
        var index = new Dictionary<string, IndexedEntry>(StringComparer.Ordinal);

        foreach (var entry in entries)
        {
            RegisterCanonicalEntry(entry, index);
        }

        return index;
    }

    private static List<IndexedEntry> SelectViewerRrEntries(
        IReadOnlyList<IndexedEntry> rrEntries,
        IReadOnlyDictionary<string, IndexedEntry> rrIndex)
    {
        var selectedReferences = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Queue<IndexedEntry>();

        foreach (var entry in rrEntries)
        {
            if (HasProfile(entry.Resource, ProcessingStatusProfile) ||
                HasProfile(entry.Resource, ProcessingStatusReasonProfile) ||
                (HasProfile(entry.Resource, RelevantConditionProfile) &&
                    HasMeaningfulObservationValue(entry.Resource)))
            {
                Enqueue(entry, selectedReferences, pending);
            }
        }

        while (pending.TryDequeue(out var current))
        {
            foreach (var reference in GetReferences(current.Resource))
            {
                if (rrIndex.TryGetValue(reference, out var referencedEntry) &&
                    HasViewerRrProfile(referencedEntry.Resource))
                {
                    Enqueue(referencedEntry, selectedReferences, pending);
                }
            }
        }

        return rrEntries
            .Where(entry => selectedReferences.Contains(entry.Reference))
            .ToList();
    }

    private static void Enqueue(
        IndexedEntry entry,
        HashSet<string> selectedReferences,
        Queue<IndexedEntry> pending)
    {
        if (selectedReferences.Add(entry.Reference))
        {
            pending.Enqueue(entry);
        }
    }

    private static bool HasMeaningfulObservationValue(Resource resource)
    {
        if (resource is not Observation { Value: { } value })
        {
            return false;
        }

        return value is not CodeableConcept codeableConcept ||
            !codeableConcept.Coding.Any(coding =>
                string.Equals(coding.Code, "NA", StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasViewerRrProfile(Resource resource)
    {
        return GetProfiles(resource).Any(profile => ViewerRrProfiles.Contains(profile));
    }

    private static Dictionary<string, string> BuildReferenceMap(
        IEnumerable<IndexedEntry> entries)
    {
        var referenceMap = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var entry in entries)
        {
            referenceMap.TryAdd(entry.Reference, entry.Reference);
            if (!string.IsNullOrWhiteSpace(entry.FullUrl))
            {
                referenceMap.TryAdd(entry.FullUrl, entry.Reference);
            }
        }

        return referenceMap;
    }

    private static IReadOnlyList<IndexedEntry> ReconcileIdentityCollisions(
        IReadOnlyList<IndexedEntry> selectedRrEntries,
        Dictionary<string, IndexedEntry> entriesByAlias,
        Dictionary<string, string> referenceMap)
    {
        var entriesToAppend = new List<IndexedEntry>();
        var collisionPairs = new List<(IndexedEntry Retained, IndexedEntry Candidate)>();

        foreach (var rrEntry in selectedRrEntries)
        {
            var matchingEntries = GetIdentityAliases(rrEntry)
                .Where(entriesByAlias.ContainsKey)
                .Select(alias => entriesByAlias[alias])
                .Distinct()
                .ToList();

            if (matchingEntries.Count == 0)
            {
                RegisterCanonicalEntry(rrEntry, entriesByAlias);
                entriesToAppend.Add(rrEntry);
                continue;
            }

            if (matchingEntries.Count != 1)
            {
                throw CreateIdentityCollisionException();
            }

            var retainedEntry = matchingEntries[0];
            collisionPairs.Add((retainedEntry, rrEntry));

            foreach (var alias in GetIdentityAliases(rrEntry))
            {
                entriesByAlias[alias] = retainedEntry;
                referenceMap[alias] = retainedEntry.Reference;
            }
        }

        if (collisionPairs.Any(pair =>
            !HaveEquivalentResourceContent(pair.Retained, pair.Candidate, referenceMap)))
        {
            throw CreateIdentityCollisionException();
        }

        foreach (var (retained, candidate) in collisionPairs)
        {
            var addedProfiles = MergeProfiles(retained.Resource, candidate.Resource);
            MarkRetainedEicrResource(retained.Resource, addedProfiles);
        }

        return entriesToAppend;
    }

    private static void RegisterCanonicalEntry(
        IndexedEntry entry,
        IDictionary<string, IndexedEntry> entriesByAlias)
    {
        foreach (var alias in GetIdentityAliases(entry))
        {
            if (entriesByAlias.TryGetValue(alias, out var existingEntry) &&
                !ReferenceEquals(existingEntry, entry))
            {
                throw CreateIdentityCollisionException();
            }

            entriesByAlias[alias] = entry;
        }
    }

    private static IEnumerable<string> GetIdentityAliases(IndexedEntry entry)
    {
        yield return entry.Reference;

        if (!string.IsNullOrWhiteSpace(entry.FullUrl) && entry.FullUrl != entry.Reference)
        {
            yield return entry.FullUrl;
        }
    }

    private static bool HaveEquivalentResourceContent(
        IndexedEntry retainedEntry,
        IndexedEntry candidateEntry,
        Dictionary<string, string> referenceMap)
    {
        var comparisonMap = new Dictionary<string, string>(referenceMap, StringComparer.Ordinal);
        foreach (var alias in GetIdentityAliases(candidateEntry))
        {
            comparisonMap[alias] = retainedEntry.Reference;
        }

        var retainedResource = CreateComparableResource(retainedEntry.Resource, comparisonMap);
        var candidateResource = CreateComparableResource(candidateEntry.Resource, comparisonMap);

        // Collision reconciliation compares modeled R4 content. Recoverable-parser
        // overflow represents malformed input and is outside the merge contract.
        return PocoEqualityComparisons.IsExactly(retainedResource, candidateResource);
    }

    private static Resource CreateComparableResource(
        Resource resource,
        IReadOnlyDictionary<string, string> referenceMap)
    {
        var comparableResource = resource.DeepCopy();

        // IDs can be local aliases for the same fullUrl, while profiles describe
        // document-specific roles and do not change the resource's clinical content.
        if (comparableResource.IdElement is not null)
        {
            comparableResource.IdElement.Value = null;
        }

        if (comparableResource.Meta is { } meta)
        {
            meta.ProfileElement.Clear();
            if (!meta.EnumerateElements().Any())
            {
                comparableResource.Meta = null;
            }
        }

        RewriteReferences(comparableResource, referenceMap);
        return comparableResource;
    }

    private static UserFacingException CreateIdentityCollisionException()
    {
        return new UserFacingException(
            "FHIR eICR and RR Bundles contain conflicting resource identities.",
            HttpStatusCode.UnprocessableEntity);
    }

    private static void MapRrPatientToEicrPatient(
        IndexedEntry eicrPatient,
        IndexedEntry rrPatient,
        IReadOnlyDictionary<string, IndexedEntry> eicrEntriesByAlias,
        IDictionary<string, string> referenceMap)
    {
        var patientAliases = GetIdentityAliases(rrPatient).ToList();
        if (patientAliases.Any(alias =>
            eicrEntriesByAlias.TryGetValue(alias, out var existingEntry) &&
            !ReferenceEquals(existingEntry, eicrPatient)))
        {
            throw CreateIdentityCollisionException();
        }

        foreach (var alias in patientAliases)
        {
            referenceMap[alias] = eicrPatient.Reference;
        }
    }

    private static void RewriteReferences(
        Base node,
        IReadOnlyDictionary<string, string> referenceMap)
    {
        foreach (var reference in DescendantsAndSelf(node).OfType<ResourceReference>())
        {
            if (reference.Reference is not { } currentReference ||
                !referenceMap.TryGetValue(currentReference, out var replacement))
            {
                continue;
            }

            reference.ReferenceElement ??= new FhirString();
            reference.ReferenceElement.Value = replacement;
        }
    }

    private static IEnumerable<string> GetReferences(Base node)
    {
        return DescendantsAndSelf(node)
            .OfType<ResourceReference>()
            .Select(reference => reference.Reference)
            .Where(reference => !string.IsNullOrWhiteSpace(reference))
            .Cast<string>();
    }

    private static IEnumerable<Base> DescendantsAndSelf(Base root)
    {
        yield return root;

        foreach (var (_, value) in root.EnumerateElements())
        {
            switch (value)
            {
                case Base child:
                    foreach (var descendant in DescendantsAndSelf(child))
                    {
                        yield return descendant;
                    }

                    break;
                case IReadOnlyList<Base?> children:
                    foreach (var child in children)
                    {
                        if (child is null)
                        {
                            continue;
                        }

                        foreach (var descendant in DescendantsAndSelf(child))
                        {
                            yield return descendant;
                        }
                    }

                    break;
            }
        }
    }

    private static void AppendSelectedEntries(
        ICollection<Bundle.EntryComponent> eicrEntries,
        IEnumerable<IndexedEntry> entriesToAppend)
    {
        foreach (var entry in entriesToAppend)
        {
            var appendedEntry = entry.Entry.DeepCopy();
            if (appendedEntry.Resource is not null)
            {
                MarkAppendedRrResource(appendedEntry.Resource);
            }

            eicrEntries.Add(appendedEntry);
        }
    }

    private static void ReplaceReportabilityResponseSection(
        Composition eicrComposition,
        Composition rrComposition,
        IReadOnlyList<IndexedEntry> selectedRrEntries,
        IReadOnlyDictionary<string, string> referenceMap)
    {
        eicrComposition.Section.RemoveAll(IsReportabilityResponseSection);

        var rrSection = new Composition.SectionComponent
        {
            ElementId = ReportabilityResponseSectionId,
            Title = ReportabilityResponseSectionTitle,
            Text = new Narrative
            {
                Status = Narrative.NarrativeStatus.Generated,
                Div =
                    "<div xmlns=\"http://www.w3.org/1999/xhtml\">Reportability Response Information Section</div>",
            },
            Code = CreateReportabilityResponseCode(),
        };

        rrSection.Extension.Add(new Extension
        {
            Url = InitiationTypeExtension,
            Value = CreateInitiationType(),
        });

        var processingStatusExtension = GetProcessingStatusExtension(
            rrComposition,
            selectedRrEntries,
            referenceMap);

        if (processingStatusExtension is not null)
        {
            rrSection.Extension.Add(processingStatusExtension);
        }

        foreach (var condition in selectedRrEntries.Where(entry =>
            HasProfile(entry.Resource, RelevantConditionProfile)))
        {
            var conditionReference = new ResourceReference
            {
                Reference = ResolveReference(condition.Reference, referenceMap),
            };

            if (GetConditionDisplay(condition.Resource) is { } display)
            {
                conditionReference.Display =
                    $"Relevant Reportable Condition Observation - {display}";
            }

            rrSection.Entry.Add(conditionReference);
        }

        eicrComposition.Section.Add(rrSection);
    }

    private static Extension? GetProcessingStatusExtension(
        Composition rrComposition,
        IReadOnlyList<IndexedEntry> selectedRrEntries,
        IReadOnlyDictionary<string, string> referenceMap)
    {
        var processingStatus = selectedRrEntries.FirstOrDefault(entry =>
            HasProfile(entry.Resource, ProcessingStatusProfile));
        var processingStatusExtension =
            FindExtension(rrComposition, ProcessingStatusExtension)?.DeepCopy();

        if (processingStatusExtension is null)
        {
            return CreateProcessingStatusExtension(processingStatus, referenceMap);
        }

        if (processingStatus is not null &&
            FindExtension(processingStatusExtension, "eICRProcessingStatus")?.Value
                is ResourceReference statusReference &&
            string.IsNullOrWhiteSpace(statusReference.Display) &&
            GetCodeDisplay(processingStatus.Resource) is { } display)
        {
            statusReference.DisplayElement ??= new FhirString();
            statusReference.DisplayElement.Value = display;
        }

        return processingStatusExtension;
    }

    private static CodeableConcept CreateInitiationType()
    {
        var initiationType = CreateReportabilityResponseCode();
        initiationType.Text = "official";
        return initiationType;
    }

    private static CodeableConcept CreateReportabilityResponseCode()
    {
        return new CodeableConcept
        {
            Coding =
            {
                new Coding
                {
                    Code = ReportabilityResponseCode,
                    System = "http://loinc.org",
                    Display = "Reportability response report Document Public health",
                },
            },
        };
    }

    private static Extension? CreateProcessingStatusExtension(
        IndexedEntry? processingStatus,
        IReadOnlyDictionary<string, string> referenceMap)
    {
        if (processingStatus is null)
        {
            return null;
        }

        var statusReference = new ResourceReference
        {
            Reference = ResolveReference(processingStatus.Reference, referenceMap),
            Display = GetCodeDisplay(processingStatus.Resource),
        };

        var processingStatusReferenceExtension = new Extension
        {
            Url = "eICRProcessingStatus",
            Value = statusReference,
        };

        var extension = new Extension
        {
            Url = ProcessingStatusExtension,
        };
        extension.Extension.Add(processingStatusReferenceExtension);

        return extension;
    }

    private static string ResolveReference(
        string reference,
        IReadOnlyDictionary<string, string> referenceMap)
    {
        return referenceMap.TryGetValue(reference, out var resolvedReference)
            ? resolvedReference
            : reference;
    }

    private static Extension? FindExtension(Base node, string extensionUrl)
    {
        return DescendantsAndSelf(node)
            .OfType<Extension>()
            .FirstOrDefault(extension => extension.Url == extensionUrl);
    }

    private static bool HasCode(CodeableConcept? codeableConcept, string code)
    {
        return codeableConcept?.Coding.Any(coding => coding.Code == code) == true;
    }

    private static string? GetConditionDisplay(Resource resource)
    {
        if (resource is not Observation { Value: CodeableConcept value })
        {
            return null;
        }

        return value.Text
            ?? GetFirstCodingValue(value, coding => coding.Display)
            ?? GetFirstCodingValue(value, coding => coding.Code);
    }

    private static string? GetCodeDisplay(Resource resource)
    {
        return resource is Observation observation
            ? GetFirstCodingValue(observation.Code, coding => coding.Display)
                ?? GetFirstCodingValue(observation.Code, coding => coding.Code)
            : null;
    }

    private static string? GetFirstCodingValue(
        CodeableConcept? codeableConcept,
        Func<Coding, string?> selector)
    {
        return codeableConcept?.Coding
            .Select(selector)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }

    private static IReadOnlyList<string> MergeProfiles(
        Resource retainedResource,
        Resource candidateResource)
    {
        var addedProfiles = new List<string>();
        if (candidateResource.Meta is null)
        {
            return addedProfiles;
        }

        retainedResource.Meta ??= new Meta();

        var existingProfiles = retainedResource.Meta.ProfileElement
            .Select(profile => profile.JsonValue as string)
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);

        foreach (var profileElement in candidateResource.Meta.ProfileElement)
        {
            if (profileElement.JsonValue is string profile &&
                existingProfiles.Add(profile))
            {
                retainedResource.Meta.ProfileElement.Add(profileElement.DeepCopy());
                addedProfiles.Add(profile);
            }
        }

        return addedProfiles;
    }

    private static void MarkRetainedEicrResource(
        Resource resource,
        IEnumerable<string> addedProfiles)
    {
        AddResourceTag(
            resource,
            ResourceOwnershipTagSystem,
            RetainedEicrResourceTagCode);

        foreach (var profile in addedProfiles)
        {
            AddResourceTag(resource, AddedRrProfileTagSystem, profile);
        }
    }

    private static void MarkAppendedRrResource(Resource resource)
    {
        AddResourceTag(
            resource,
            ResourceOwnershipTagSystem,
            AppendedRrResourceTagCode);
    }

    private static void AddResourceTag(Resource resource, string system, string code)
    {
        resource.Meta ??= new Meta();

        if (!HasTag(resource.Meta.Tag, system, code))
        {
            resource.Meta.Tag.Add(new Coding
            {
                System = system,
                Code = code,
            });
        }
    }

    private static bool HasRetainedEicrResourceTag(Resource resource)
    {
        return HasResourceTag(resource, RetainedEicrResourceTagCode);
    }

    private static bool HasAppendedRrResourceTag(Resource resource)
    {
        return HasResourceTag(resource, AppendedRrResourceTagCode);
    }

    private static bool HasResourceTag(Resource resource, string code)
    {
        return resource.Meta is not null &&
            HasTag(resource.Meta.Tag, ResourceOwnershipTagSystem, code);
    }

    private static bool HasTag(IEnumerable<Coding> tags, string system, string code)
    {
        return tags.Any(tag => tag.System == system && tag.Code == code);
    }

    private static void RestoreRetainedEicrResource(Resource resource)
    {
        if (resource.Meta is not { } meta)
        {
            return;
        }

        var addedProfiles = meta.Tag
            .Where(tag => tag.System == AddedRrProfileTagSystem)
            .Select(tag => tag.Code)
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);

        meta.ProfileElement.RemoveAll(profile =>
            profile.JsonValue is string value &&
            addedProfiles.Contains(value));

        meta.Tag.RemoveAll(tag =>
            tag.System == AddedRrProfileTagSystem ||
            (tag.System == ResourceOwnershipTagSystem &&
                tag.Code == RetainedEicrResourceTagCode));

        if (!meta.EnumerateElements().Any())
        {
            resource.Meta = null;
        }
    }

    private static bool HasProfile(Resource resource, string expectedProfile)
    {
        return GetProfiles(resource).Contains(expectedProfile, StringComparer.Ordinal);
    }

    private static IEnumerable<string> GetProfiles(Resource resource)
    {
        if (resource.Meta is null)
        {
            yield break;
        }

        foreach (var profile in resource.Meta.ProfileElement
            .Select(profile => profile.JsonValue as string)
            .OfType<string>())
        {
            if (!string.IsNullOrWhiteSpace(profile))
            {
                yield return GetProfileUrl(profile);
            }
        }
    }

    private static string GetProfileUrl(string profile)
    {
        var versionSeparator = profile.IndexOf('|');
        return versionSeparator >= 0 ? profile[..versionSeparator] : profile;
    }

    private sealed class IndexedEntry
    {
        public IndexedEntry(
            Bundle.EntryComponent entry,
            Resource resource,
            string reference,
            string? fullUrl)
        {
            Entry = entry;
            Resource = resource;
            Reference = reference;
            FullUrl = fullUrl;
        }

        public Bundle.EntryComponent Entry { get; }

        public Resource Resource { get; }

        public string Reference { get; }

        public string? FullUrl { get; }
    }
}
