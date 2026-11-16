// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Dibbs.Fhir.Liquid.Converter.Exceptions;
using Dibbs.Fhir.Liquid.Converter.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dibbs.Fhir.Liquid.Converter.OutputProcessors
{
    public static class PostProcessor
    {
        public static JObject Process(string input)
        {
            var jsonObject = ParseJson(input);
            return MergeJson(jsonObject);
        }

        public static JObject ParseJson(string input)
        {
            return new BundleJsonParser(input).Parse();
        }

        public static JObject MergeJson(JObject obj)
        {
            try
            {
                var mergedEntity = new JArray();
                var resourceKeyToIndexMap = new Dictionary<string, int>();

                if (obj.TryGetValue("entry", out var entries))
                {
                    // Detach in one pass: adding already-parented tokens to another
                    // container clones them, and removing entries individually is quadratic.
                    var originalEntries = entries.ToArray();
                    if (entries is JContainer container)
                    {
                        container.RemoveAll();
                    }

                    var settings = new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Union };
                    foreach (var entry in originalEntries)
                    {
                        var resourceKey = GetKey(entry);
                        if (resourceKeyToIndexMap.TryGetValue(resourceKey, out var index))
                        {
                            // Merge mutates the existing resource. Assigning it back would clone it again.
                            ((JObject)mergedEntity[index]).Merge((JObject)entry, settings);
                        }
                        else
                        {
                            mergedEntity.Add(entry);
                            resourceKeyToIndexMap[resourceKey] = mergedEntity.Count - 1;
                        }
                    }

                    obj["entry"] = mergedEntity;
                }
            }
            catch (Exception ex)
            {
                throw new PostprocessException(FhirConverterErrorCode.JsonMergingError, string.Format(Resources.JsonMergingError, ex.Message), ex);
            }

            return obj;
        }

        private static string GetKey(JToken obj)
        {
            var resourceType = obj.SelectToken("$.resource.resourceType")?.Value<string>();
            if (resourceType != null)
            {
                var key = resourceType;
                var versionId = obj.SelectToken("$.resource.meta.versionId")?.Value<string>();
                key += versionId != null ? $"_{versionId}" : string.Empty;

                var resourceId = obj.SelectToken("$.resource.id")?.Value<string>();
                key += resourceId != null ? $"_{resourceId}" : string.Empty;

                return key;
            }

            return JsonConvert.SerializeObject(obj);
        }
    }
}
