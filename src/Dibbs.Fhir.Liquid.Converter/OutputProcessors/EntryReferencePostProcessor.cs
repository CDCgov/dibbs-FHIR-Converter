// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;

namespace Dibbs.Fhir.Liquid.Converter.OutputProcessors
{
    public static class EntryReferencePostProcessor
    {
        private const string EntryReferenceUrl = "https://github.com/CDCgov/dibbs-FHIR-Converter/StructureDefinition/cda-entry-reference";
        private const string ActRelationshipCodeSystem = "http://terminology.hl7.org/CodeSystem/v3-ActRelationshipType";
        private const string EntryReferenceTemplateId = "2.16.840.1.113883.10.20.22.4.122";

        public static string Process(string bundleJsonString, string cdaData)
        {
            // Skip adding entry reference extensions if entry reference OID does not exist in document.
            if (!cdaData.Contains(EntryReferenceTemplateId, StringComparison.Ordinal))
            {
                return bundleJsonString;
            }

            return Process(bundleJsonString, XDocument.Parse(cdaData));
        }

        public static string Process(string bundleJsonString, XDocument cdaDocument)
        {
            return Process(bundleJsonString, Capture(cdaDocument));
        }

        internal static string Process(string bundleJsonString, IReadOnlyList<EntryReference> entryReferenceActs)
        {
            // The entry reference OID appeared in the document but was not the templateId of an act element.
            // Do not add entry reference extensions.
            if (entryReferenceActs.Count == 0)
            {
                return bundleJsonString;
            }

            var bundleNode = JsonNode.Parse(bundleJsonString);
            if (bundleNode == null || !ResolveEntryReferences(bundleNode, entryReferenceActs))
            {
                return bundleJsonString;
            }

            return bundleNode.ToJsonString(new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            });
        }

        public static JsonNode Process(JsonNode bundle, XDocument cdaDocument)
        {
            ResolveEntryReferences(bundle, Capture(cdaDocument));
            return bundle;
        }

        private static bool ResolveEntryReferences(
            JsonNode bundle,
            IEnumerable<EntryReference> entryReferenceActs)
        {
            if (bundle["entry"] is not JsonArray entries)
            {
                return false;
            }

            var resourceByIdentifier = BuildResourceIndex(entries);

            var updated = false;
            foreach (var entryReferenceAct in entryReferenceActs)
            {
                if (FindMatchingResource(entryReferenceAct.TargetIds, resourceByIdentifier) is not { } target ||
                    FindMatchingResource(entryReferenceAct.AncestorIds, resourceByIdentifier) is not { } source)
                {
                    continue;
                }

                AddEntryReferenceExtension(source.Resource, target, entryReferenceAct);
                updated = true;
            }

            return updated;
        }

        private static Dictionary<string, ResourceMatch?> BuildResourceIndex(JsonArray entries)
        {
            // Match CDA ids to the resources created from those clinical statements.
            var index = new Dictionary<string, ResourceMatch?>(StringComparer.OrdinalIgnoreCase);

            foreach (var resource in entries
                .Select(entry => entry?["resource"])
                .OfType<JsonObject>())
            {
                var resourceType = resource["resourceType"]?.GetValue<string>();
                var resourceId = resource["id"]?.GetValue<string>();
                if (string.IsNullOrEmpty(resourceType) || string.IsNullOrEmpty(resourceId))
                {
                    continue;
                }

                var resourceMatch = new ResourceMatch(resourceType, resourceId, resource);
                foreach (var identifier in GetResourceIdentifiers(resource))
                {
                    var key = GetFhirIdKey(identifier);
                    if (key == null)
                    {
                        continue;
                    }

                    if (!index.TryAdd(key, resourceMatch) &&
                        index[key] is { } existing &&
                        existing.Reference != resourceMatch.Reference)
                    {
                        Console.Out.WriteLine($"More than one resource found with ID {key}");
                    }
                }
            }

            return index;
        }

        private static IEnumerable<JsonObject> GetResourceIdentifiers(JsonObject resource) =>
            resource["identifier"] switch
            {
                JsonArray identifiers => identifiers.OfType<JsonObject>(),
                JsonObject identifier => new[] { identifier },
                _ => Enumerable.Empty<JsonObject>(),
            };

        // Copy only identifiers and relationship attributes before the parser mutates the XML.
        internal static IReadOnlyList<EntryReference> Capture(XDocument document)
        {
            return FindEntryReferenceActs(document).Select(act =>
            {
                var relationship = act.Ancestors().FirstOrDefault(a => a.Name.LocalName == "entryRelationship");
                return new EntryReference(
                    GetIdentifierKeys(act).ToArray(),
                    GetAncestorIdentifierKeys(act).ToArray(),
                    relationship?.Attribute("typeCode")?.Value?.Trim(),
                    relationship?.Attribute("inversionInd")?.Value?.Trim()?.ToLowerInvariant());
            }).ToArray();
        }

        private static IEnumerable<string> GetIdentifierKeys(XElement element)
        {
            return element.Elements().Where(e => e.Name.LocalName == "id")
                .Select(GetCdaIdKey).Where(key => key != null);
        }

        private static IEnumerable<string> GetAncestorIdentifierKeys(XElement act)
        {
            foreach (var ancestor in act.Ancestors())
            {
                foreach (var key in GetIdentifierKeys(ancestor))
                {
                    yield return key;
                }

                // The containing section is eligible; ancestors above it are not.
                if (ancestor.Name.LocalName == "section")
                {
                    yield break;
                }
            }
        }

        private static void AddEntryReferenceExtension(
            JsonObject sourceResource,
            ResourceMatch target,
            EntryReference entryReferenceAct)
        {
            var typeCode = entryReferenceAct.TypeCode;
            var inversionInd = entryReferenceAct.InversionInd;

            var extensionParts = new JsonArray
            {
                new JsonObject
                {
                    ["url"] = "target",
                    ["valueReference"] = new JsonObject { ["reference"] = target.Reference },
                },
            };

            if (!string.IsNullOrEmpty(typeCode))
            {
                extensionParts.Add(new JsonObject
                {
                    ["url"] = "typeCode",
                    ["valueCoding"] = new JsonObject
                    {
                        ["system"] = ActRelationshipCodeSystem,
                        ["code"] = typeCode,
                    },
                });
            }

            if (inversionInd is "true" or "1" or "false" or "0")
            {
                extensionParts.Add(new JsonObject
                {
                    ["url"] = "inversionInd",
                    ["valueBoolean"] = inversionInd is "true" or "1",
                });
            }

            if (sourceResource["extension"] is not JsonArray extensions)
            {
                extensions = new JsonArray();
                sourceResource["extension"] = extensions;
            }

            extensions.Add(new JsonObject
            {
                ["url"] = EntryReferenceUrl,
                ["extension"] = extensionParts,
            });
        }

        private static IEnumerable<XElement> FindEntryReferenceActs(XDocument cdaDocument)
        {
            return cdaDocument.Descendants()
                .Where(element => element.Name.LocalName == "act" &&
                    element.Elements().Any(templateId => templateId.Name.LocalName == "templateId" &&
                        string.Equals(templateId.Attribute("root")?.Value?.Trim(), EntryReferenceTemplateId, StringComparison.Ordinal)));
        }

        private static string? GetFhirIdKey(JsonObject identifier)
        {
            var system = NormalizeId(identifier["system"]?.GetValue<string>());
            var value = NormalizeId(identifier["value"]?.GetValue<string>());

            // RFC 3986 identifiers keep the CDA root in value instead of system.
            if (string.Equals(system, "urn:ietf:rfc:3986", StringComparison.OrdinalIgnoreCase))
            {
                system = null;
            }

            return BuildIdentifierKey(system, value);
        }

        private static ResourceMatch? FindMatchingResource(
            IEnumerable<string> keys,
            Dictionary<string, ResourceMatch?> index)
        {
            foreach (var key in keys)
            {
                if (index.TryGetValue(key, out var matched) && matched != null)
                {
                    return matched;
                }
            }

            return null;
        }

        private static string? GetCdaIdKey(XElement id)
        {
            if (id.Attribute("nullFlavor") != null)
            {
                return null;
            }

            var root = NormalizeId(id.Attribute("root")?.Value);
            if (root == null)
            {
                return null;
            }

            return BuildIdentifierKey(root, NormalizeId(id.Attribute("extension")?.Value));
        }

        private static string? BuildIdentifierKey(string? root, string? extension)
        {
            return root != null && extension != null
                ? $"{root}\u001f{extension}"
                : extension ?? root;
        }

        private static string? NormalizeId(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            id = id.Trim();
            if (id.StartsWith("urn:oid:", StringComparison.OrdinalIgnoreCase))
            {
                return id["urn:oid:".Length..].Trim();
            }

            if (id.StartsWith("urn:uuid:", StringComparison.OrdinalIgnoreCase))
            {
                return id["urn:uuid:".Length..].Trim();
            }

            return id;
        }

        internal sealed class EntryReference(string[] targetIds, string[] ancestorIds, string? typeCode, string? inversionInd)
        {
            public string[] TargetIds { get; } = targetIds;

            public string[] AncestorIds { get; } = ancestorIds;

            public string? TypeCode { get; } = typeCode;

            public string? InversionInd { get; } = inversionInd;
        }

        private sealed class ResourceMatch(string resourceType, string id, JsonObject resource)
        {
            public JsonObject Resource { get; } = resource;

            public string Reference { get; } = $"{resourceType}/{id}";
        }
    }
}
