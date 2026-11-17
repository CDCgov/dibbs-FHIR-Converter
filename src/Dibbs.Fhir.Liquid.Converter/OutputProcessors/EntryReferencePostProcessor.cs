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
            var entryReferenceActs = FindEntryReferenceActs(cdaDocument).ToList();

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
            ResolveEntryReferences(bundle, FindEntryReferenceActs(cdaDocument));
            return bundle;
        }

        private static bool ResolveEntryReferences(
            JsonNode bundle,
            IEnumerable<XElement> entryReferenceActs)
        {
            if (bundle["entry"] is not JsonArray entries)
            {
                return false;
            }

            var resourceByIdentifier = BuildResourceIndex(entries);

            var updated = false;
            foreach (var entryReferenceAct in entryReferenceActs)
            {
                if (FindMatchingResource(entryReferenceAct, resourceByIdentifier) is not { } target ||
                    FindContainingResource(entryReferenceAct, resourceByIdentifier) is not { } source)
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

        private static ResourceMatch? FindContainingResource(
            XElement entryReferenceAct,
            Dictionary<string, ResourceMatch?> index)
        {
            // The nearest converted ancestor is the resource that contains the Entry Reference.
            foreach (var ancestor in entryReferenceAct.Ancestors())
            {
                var matched = FindMatchingResource(ancestor, index);

                // Check the containing section itself, but do not search beyond it.
                if (matched != null || ancestor.Name.LocalName == "section")
                {
                    return matched;
                }
            }

            return null;
        }

        private static void AddEntryReferenceExtension(
            JsonObject sourceResource,
            ResourceMatch target,
            XElement entryReferenceAct)
        {
            var relationship = entryReferenceAct.Ancestors().FirstOrDefault(a => a.Name.LocalName == "entryRelationship");
            var typeCode = relationship?.Attribute("typeCode")?.Value?.Trim();
            var inversionInd = relationship?.Attribute("inversionInd")?.Value?.Trim()?.ToLowerInvariant();

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
            XElement element,
            Dictionary<string, ResourceMatch?> index)
        {
            foreach (var id in element.Elements().Where(element => element.Name.LocalName == "id"))
            {
                var key = GetCdaIdKey(id);

                if (key != null && index.TryGetValue(key, out var matched) && matched != null)
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

        private sealed class ResourceMatch(string resourceType, string id, JsonObject resource)
        {
            public JsonObject Resource { get; } = resource;

            public string Reference { get; } = $"{resourceType}/{id}";
        }
    }
}
