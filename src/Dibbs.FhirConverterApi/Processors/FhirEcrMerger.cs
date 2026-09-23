using System.Net;
using System.Text.Json.Nodes;
using Dibbs.FhirConverterApi.Models;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

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
    /// Merges the RR resources used by the Viewer into the eICR document Bundle.
    /// </summary>
    /// <param name="eicrBundle">The eICR document Bundle.</param>
    /// <param name="rrBundle">The RR document Bundle.</param>
    /// <returns>The merged eICR Bundle serialized as FHIR JSON.</returns>
    public static string Merge(Bundle eicrBundle, Bundle rrBundle)
    {
        var eicrJson = ParseBundle(eicrBundle);
        var rrJson = ParseBundle(rrBundle);

        EnsureBundleId(eicrJson);

        var eicrEntries = GetOrCreateEntries(eicrJson);
        var rrEntries = GetOrCreateEntries(rrJson);
        RemoveExistingViewerRrEntries(eicrEntries);

        var indexedEicrEntries = IndexEntries(eicrEntries);
        var indexedRrEntries = IndexEntries(rrEntries);
        var (eicrComposition, rrComposition) = ValidateMergeInputs(
            eicrJson,
            rrJson,
            indexedEicrEntries,
            indexedRrEntries);

        var rrIndex = BuildEntryIndex(indexedRrEntries);
        var selectedRrEntries = SelectViewerRrEntries(indexedRrEntries, rrIndex);
        ValidateNoIdentityCollisions(indexedEicrEntries, selectedRrEntries);
        var referenceMap = BuildReferenceMap(indexedEicrEntries, selectedRrEntries);

        MapRrPatientToEicrPatient(indexedEicrEntries, indexedRrEntries, referenceMap);
        RewriteReferences(eicrJson, referenceMap);
        RewriteReferences(rrJson, referenceMap);

        AppendSelectedEntries(eicrEntries, indexedEicrEntries, selectedRrEntries);
        ReplaceReportabilityResponseSection(
            eicrComposition.Resource,
            rrComposition.Resource,
            selectedRrEntries);

        return eicrJson.ToJsonString();
    }

    private static JsonObject ParseBundle(Bundle bundle)
    {
        return JsonNode.Parse(bundle.ToJson()) as JsonObject
            ?? throw new InvalidOperationException("Unable to serialize the FHIR Bundle.");
    }

    private static void EnsureBundleId(JsonObject bundle)
    {
        if (string.IsNullOrWhiteSpace(GetString(bundle["id"])))
        {
            bundle["id"] = Guid.NewGuid().ToString();
        }
    }

    private static JsonArray GetOrCreateEntries(JsonObject bundle)
    {
        if (bundle["entry"] is JsonArray entries)
        {
            return entries;
        }

        entries = new JsonArray();
        bundle["entry"] = entries;
        return entries;
    }

    private static List<IndexedEntry> IndexEntries(JsonArray entries)
    {
        var indexedEntries = new List<IndexedEntry>();

        foreach (var entry in entries.OfType<JsonObject>())
        {
            if (entry["resource"] is not JsonObject resource)
            {
                continue;
            }

            var resourceType = GetString(resource["resourceType"]);
            if (string.IsNullOrWhiteSpace(resourceType))
            {
                continue;
            }

            var fullUrl = GetString(entry["fullUrl"]);
            var resourceId = GetString(resource["id"]);

            if (string.IsNullOrWhiteSpace(resourceId))
            {
                resourceId = DeriveResourceId(fullUrl);
                resource["id"] = resourceId;
            }

            indexedEntries.Add(new IndexedEntry(
                entry,
                resource,
                resourceType,
                $"{resourceType}/{resourceId}",
                fullUrl));
        }

        return indexedEntries;
    }

    private static void RemoveExistingViewerRrEntries(JsonArray entries)
    {
        for (var index = entries.Count - 1; index >= 0; index--)
        {
            if (entries[index]?["resource"] is JsonObject resource &&
                HasViewerRrProfile(resource))
            {
                entries.RemoveAt(index);
            }
        }
    }

    private static (IndexedEntry EicrComposition, IndexedEntry RrComposition)
        ValidateMergeInputs(
            JsonObject eicrBundle,
            JsonObject rrBundle,
            IReadOnlyList<IndexedEntry> eicrEntries,
            IReadOnlyList<IndexedEntry> rrEntries)
    {
        var eicrCompositions = eicrEntries.Where(entry =>
            entry.ResourceType == "Composition" &&
            HasProfile(entry.Resource, EicrCompositionProfile)).ToList();
        var eicrPatientCount = eicrEntries.Count(entry => entry.ResourceType == "Patient");

        if (GetString(eicrBundle["type"]) != "document" ||
            eicrCompositions.Count != 1 ||
            eicrPatientCount != 1)
        {
            throw new UserFacingException(
                "FHIR eICR input must contain exactly one eICR Composition and one Patient.",
                HttpStatusCode.UnprocessableEntity);
        }

        var rrCompositions = rrEntries.Where(entry =>
            entry.ResourceType == "Composition" &&
            HasProfile(entry.Resource, RrCompositionProfile)).ToList();
        var rrPatientCount = rrEntries.Count(entry => entry.ResourceType == "Patient");

        if (GetString(rrBundle["type"]) != "document" ||
            rrCompositions.Count != 1 ||
            rrPatientCount != 1)
        {
            throw new UserFacingException(
                "FHIR RR input must contain exactly one RR Composition and one Patient.",
                HttpStatusCode.UnprocessableEntity);
        }

        return (eicrCompositions[0], rrCompositions[0]);
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

        return IsValidFhirId(candidate) ? candidate! : Guid.NewGuid().ToString();
    }

    private static bool IsValidFhirId(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
            value.Length <= 64 &&
            value.All(character =>
                (character >= 'a' && character <= 'z') ||
                (character >= 'A' && character <= 'Z') ||
                (character >= '0' && character <= '9') ||
                character is '-' or '.');
    }

    private static Dictionary<string, IndexedEntry> BuildEntryIndex(
        IEnumerable<IndexedEntry> entries)
    {
        var index = new Dictionary<string, IndexedEntry>(StringComparer.Ordinal);

        foreach (var entry in entries)
        {
            index.TryAdd(entry.Reference, entry);
            if (!string.IsNullOrWhiteSpace(entry.FullUrl))
            {
                index.TryAdd(entry.FullUrl, entry);
            }
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

    private static bool HasMeaningfulObservationValue(JsonObject observation)
    {
        var hasValue = observation.Any(property =>
            property.Key.StartsWith("value", StringComparison.Ordinal) &&
            property.Value is not null);

        if (!hasValue)
        {
            return false;
        }

        if (observation["valueCodeableConcept"]?["coding"] is not JsonArray codings)
        {
            return true;
        }

        return !codings
            .OfType<JsonObject>()
            .Select(coding => GetString(coding["code"]))
            .Any(code => string.Equals(code, "NA", StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasViewerRrProfile(JsonObject resource)
    {
        return GetProfiles(resource).Any(profile => ViewerRrProfiles.Contains(profile));
    }

    private static Dictionary<string, string> BuildReferenceMap(
        IEnumerable<IndexedEntry> eicrEntries,
        IEnumerable<IndexedEntry> rrEntries)
    {
        var referenceMap = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var entry in eicrEntries.Concat(rrEntries))
        {
            referenceMap.TryAdd(entry.Reference, entry.Reference);
            if (!string.IsNullOrWhiteSpace(entry.FullUrl))
            {
                referenceMap.TryAdd(entry.FullUrl, entry.Reference);
            }
        }

        return referenceMap;
    }

    private static void ValidateNoIdentityCollisions(
        IEnumerable<IndexedEntry> eicrEntries,
        IReadOnlyList<IndexedEntry> selectedRrEntries)
    {
        var eicrReferences = eicrEntries
            .Select(entry => entry.Reference)
            .ToHashSet(StringComparer.Ordinal);
        var eicrFullUrls = eicrEntries
            .Select(entry => entry.FullUrl)
            .Where(fullUrl => !string.IsNullOrWhiteSpace(fullUrl))
            .ToHashSet(StringComparer.Ordinal);
        var rrReferences = new HashSet<string>(StringComparer.Ordinal);
        var rrFullUrls = new HashSet<string>(StringComparer.Ordinal);

        foreach (var rrEntry in selectedRrEntries)
        {
            var referenceCollision = !rrReferences.Add(rrEntry.Reference) ||
                eicrReferences.Contains(rrEntry.Reference);
            var fullUrlCollision = !string.IsNullOrWhiteSpace(rrEntry.FullUrl) &&
                (!rrFullUrls.Add(rrEntry.FullUrl) || eicrFullUrls.Contains(rrEntry.FullUrl));

            if (referenceCollision || fullUrlCollision)
            {
                throw new UserFacingException(
                    "FHIR eICR and RR Bundles contain conflicting resource identities.",
                    HttpStatusCode.UnprocessableEntity);
            }
        }
    }

    private static void MapRrPatientToEicrPatient(
        IEnumerable<IndexedEntry> eicrEntries,
        IEnumerable<IndexedEntry> rrEntries,
        IDictionary<string, string> referenceMap)
    {
        var eicrPatient = eicrEntries.FirstOrDefault(entry => entry.ResourceType == "Patient");
        if (eicrPatient is null)
        {
            return;
        }

        foreach (var rrPatient in rrEntries.Where(entry => entry.ResourceType == "Patient"))
        {
            referenceMap[rrPatient.Reference] = eicrPatient.Reference;
            if (!string.IsNullOrWhiteSpace(rrPatient.FullUrl))
            {
                referenceMap[rrPatient.FullUrl] = eicrPatient.Reference;
            }
        }
    }

    private static void RewriteReferences(
        JsonNode? node,
        IReadOnlyDictionary<string, string> referenceMap)
    {
        switch (node)
        {
            case JsonObject jsonObject:
                foreach (var property in jsonObject.ToList())
                {
                    if (property.Key == "reference" &&
                        GetString(property.Value) is { } reference &&
                        referenceMap.TryGetValue(reference, out var replacement))
                    {
                        jsonObject[property.Key] = replacement;
                    }
                    else
                    {
                        RewriteReferences(property.Value, referenceMap);
                    }
                }

                break;
            case JsonArray jsonArray:
                foreach (var item in jsonArray)
                {
                    RewriteReferences(item, referenceMap);
                }

                break;
        }
    }

    private static void AppendSelectedEntries(
        JsonArray eicrEntries,
        IEnumerable<IndexedEntry> indexedEicrEntries,
        IEnumerable<IndexedEntry> selectedRrEntries)
    {
        var existingReferences = indexedEicrEntries
            .Select(entry => entry.Reference)
            .ToHashSet(StringComparer.Ordinal);
        var existingFullUrls = indexedEicrEntries
            .Select(entry => entry.FullUrl)
            .Where(fullUrl => !string.IsNullOrWhiteSpace(fullUrl))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var rrEntry in selectedRrEntries)
        {
            if (existingReferences.Contains(rrEntry.Reference) ||
                (!string.IsNullOrWhiteSpace(rrEntry.FullUrl) &&
                    existingFullUrls.Contains(rrEntry.FullUrl)))
            {
                continue;
            }

            eicrEntries.Add(rrEntry.Entry.DeepClone());
            existingReferences.Add(rrEntry.Reference);
            if (!string.IsNullOrWhiteSpace(rrEntry.FullUrl))
            {
                existingFullUrls.Add(rrEntry.FullUrl);
            }
        }
    }

    private static void ReplaceReportabilityResponseSection(
        JsonObject eicrComposition,
        JsonObject rrComposition,
        IReadOnlyList<IndexedEntry> selectedRrEntries)
    {
        var sections = eicrComposition["section"] as JsonArray ?? new JsonArray();
        eicrComposition["section"] = sections;

        for (var index = sections.Count - 1; index >= 0; index--)
        {
            if (sections[index] is JsonObject section &&
                (GetString(section["title"]) == ReportabilityResponseSectionTitle ||
                    HasCode(section, ReportabilityResponseCode)))
            {
                sections.RemoveAt(index);
            }
        }

        var extensions = new JsonArray
        {
            new JsonObject
            {
                ["url"] = InitiationTypeExtension,
                ["valueCodeableConcept"] = CreateInitiationType(),
            },
        };

        var processingStatusExtension = GetProcessingStatusExtension(
            rrComposition,
            selectedRrEntries);

        if (processingStatusExtension is not null)
        {
            extensions.Add(processingStatusExtension);
        }

        var rrSection = new JsonObject
        {
            ["id"] = ReportabilityResponseSectionId,
            ["title"] = ReportabilityResponseSectionTitle,
            ["text"] = new JsonObject
            {
                ["status"] = "generated",
                ["div"] =
                    "<div xmlns=\"http://www.w3.org/1999/xhtml\">Reportability Response Information Section</div>",
            },
            ["extension"] = extensions,
            ["code"] = CreateReportabilityResponseCode(),
        };

        var conditionReferences = new JsonArray();
        foreach (var condition in selectedRrEntries.Where(entry =>
            HasProfile(entry.Resource, RelevantConditionProfile)))
        {
            var reference = new JsonObject
            {
                ["reference"] = condition.Reference,
            };
            if (GetConditionDisplay(condition.Resource) is { } display)
            {
                reference["display"] = $"Relevant Reportable Condition Observation - {display}";
            }

            conditionReferences.Add(reference);
        }

        if (conditionReferences.Count > 0)
        {
            rrSection["entry"] = conditionReferences;
        }

        sections.Add(rrSection);
    }

    private static JsonObject? GetProcessingStatusExtension(
        JsonObject rrComposition,
        IReadOnlyList<IndexedEntry> selectedRrEntries)
    {
        var processingStatus = selectedRrEntries.FirstOrDefault(entry =>
            HasProfile(entry.Resource, ProcessingStatusProfile));
        var processingStatusExtension = FindExtension(rrComposition, ProcessingStatusExtension)
            ?.DeepClone() as JsonObject;

        if (processingStatusExtension is null)
        {
            return CreateProcessingStatusExtension(processingStatus);
        }

        if (processingStatus is not null &&
            FindExtension(processingStatusExtension, "eICRProcessingStatus")?["valueReference"]
                is JsonObject statusReference &&
            string.IsNullOrWhiteSpace(GetString(statusReference["display"])) &&
            GetCodeDisplay(processingStatus.Resource) is { } display)
        {
            statusReference["display"] = display;
        }

        return processingStatusExtension;
    }

    private static JsonObject CreateInitiationType()
    {
        var initiationType = CreateReportabilityResponseCode();
        initiationType["text"] = "official";
        return initiationType;
    }

    private static JsonObject CreateReportabilityResponseCode()
    {
        return new JsonObject
        {
            ["coding"] = new JsonArray
            {
                new JsonObject
                {
                    ["code"] = ReportabilityResponseCode,
                    ["system"] = "http://loinc.org",
                    ["display"] = "Reportability response report Document Public health",
                },
            },
        };
    }

    private static JsonObject? CreateProcessingStatusExtension(
        IndexedEntry? processingStatus)
    {
        if (processingStatus is null)
        {
            return null;
        }

        var statusReference = new JsonObject
        {
            ["reference"] = processingStatus.Reference,
        };
        if (GetCodeDisplay(processingStatus.Resource) is { } display)
        {
            statusReference["display"] = display;
        }

        return new JsonObject
        {
            ["url"] = ProcessingStatusExtension,
            ["extension"] = new JsonArray
            {
                new JsonObject
                {
                    ["url"] = "eICRProcessingStatus",
                    ["valueReference"] = statusReference,
                },
            },
        };
    }

    private static JsonObject? FindExtension(JsonNode? node, string extensionUrl)
    {
        switch (node)
        {
            case JsonObject jsonObject:
                if (GetString(jsonObject["url"]) == extensionUrl)
                {
                    return jsonObject;
                }

                foreach (var property in jsonObject)
                {
                    if (FindExtension(property.Value, extensionUrl) is { } extension)
                    {
                        return extension;
                    }
                }

                break;
            case JsonArray jsonArray:
                foreach (var item in jsonArray)
                {
                    if (FindExtension(item, extensionUrl) is { } extension)
                    {
                        return extension;
                    }
                }

                break;
        }

        return null;
    }

    private static bool HasCode(JsonObject resource, string code)
    {
        return resource["code"]?["coding"] is JsonArray codings &&
            codings.OfType<JsonObject>().Any(coding => GetString(coding["code"]) == code);
    }

    private static string? GetConditionDisplay(JsonObject condition)
    {
        return GetString(condition["valueCodeableConcept"]?["text"])
            ?? GetFirstCodingValue(condition["valueCodeableConcept"], "display")
            ?? GetFirstCodingValue(condition["valueCodeableConcept"], "code");
    }

    private static string? GetCodeDisplay(JsonObject resource)
    {
        return GetFirstCodingValue(resource["code"], "display")
            ?? GetFirstCodingValue(resource["code"], "code");
    }

    private static string? GetFirstCodingValue(JsonNode? codeableConcept, string propertyName)
    {
        return (codeableConcept?["coding"] as JsonArray)?
            .OfType<JsonObject>()
            .Select(coding => GetString(coding[propertyName]))
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }

    private static bool HasProfile(JsonObject resource, string expectedProfile)
    {
        return GetProfiles(resource).Contains(expectedProfile, StringComparer.Ordinal);
    }

    private static IEnumerable<string> GetProfiles(JsonObject resource)
    {
        if (resource["meta"]?["profile"] is not JsonArray profiles)
        {
            yield break;
        }

        foreach (var profileNode in profiles)
        {
            if (GetString(profileNode) is not { } profile)
            {
                continue;
            }

            var versionSeparator = profile.IndexOf('|');
            yield return versionSeparator >= 0 ? profile[..versionSeparator] : profile;
        }
    }

    private static IEnumerable<string> GetReferences(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject jsonObject:
                foreach (var property in jsonObject)
                {
                    if (property.Key == "reference" && GetString(property.Value) is { } reference)
                    {
                        yield return reference;
                    }
                    else
                    {
                        foreach (var nestedReference in GetReferences(property.Value))
                        {
                            yield return nestedReference;
                        }
                    }
                }

                break;
            case JsonArray jsonArray:
                foreach (var item in jsonArray)
                {
                    foreach (var nestedReference in GetReferences(item))
                    {
                        yield return nestedReference;
                    }
                }

                break;
        }
    }

    private static string? GetString(JsonNode? node)
    {
        return node is JsonValue value && value.TryGetValue<string>(out var result)
            ? result
            : null;
    }

    private sealed class IndexedEntry
    {
        public IndexedEntry(
            JsonObject entry,
            JsonObject resource,
            string resourceType,
            string reference,
            string? fullUrl)
        {
            Entry = entry;
            Resource = resource;
            ResourceType = resourceType;
            Reference = reference;
            FullUrl = fullUrl;
        }

        public JsonObject Entry { get; }

        public JsonObject Resource { get; }

        public string ResourceType { get; }

        public string Reference { get; }

        public string? FullUrl { get; }
    }
}
