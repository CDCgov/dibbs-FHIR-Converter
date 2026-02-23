// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Hl7.Fhir.Serialization;
using Dibbs.Fhir.Liquid.Converter.Models;
using Dibbs.Fhir.Liquid.Converter.Processors;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;
using Dibbs.Fhir.Liquid.Converter.Utilities;
using Microsoft.Extensions.FileProviders;

namespace Dibbs.Fhir.Liquid.Converter.FunctionalTests
{
    public class BaseRuleBasedFunctionalTests
    {

        private static readonly int _maxRevealDepth = 1 << 7;
        private static readonly string _ccdaDataFolder = Path.Combine(Constants.SampleDataDirectory, "eCR");
        private static readonly FhirJsonParser _fhirParser = new FhirJsonParser();
        private readonly CcdaProcessor _ccdaProcessor;

        private readonly ITestOutputHelper _output;
        private readonly IFileProvider fileProvider;

        public BaseRuleBasedFunctionalTests(ITestOutputHelper output)
        {
            this._output = output;

            _ccdaProcessor = new CcdaProcessor(FhirConverterLogging.CreateLogger<CcdaProcessor>(), TemplateUtility.TemplateOptions);
            fileProvider = new PhysicalFileProvider(Path.GetFullPath(Path.Combine(Constants.TemplateDirectory, "eCR")));
        }

        public static IEnumerable<object[]> GetCcdaRuleBasedTestCases()
        {
            var cases = new List<object[]>
            {
                new object[] { "EICR", "eCR_RR_combined_3_1.xml" },
                new object[] { "EICR", "eCR_RR_combined_special_chars.xml" },
                new object[] { "EICR", "eicr04152020.xml" },

                new object[] { @"ConsultationNote", @"eCR_RR_combined_3_1.xml" },
                new object[] { @"DischargeSummary", @"eCR_RR_combined_3_1.xml" },
                new object[] { @"HistoryandPhysical", @"eCR_RR_combined_3_1.xml" },
                new object[] { @"OperativeNote", @"eCR_RR_combined_3_1.xml" },
                new object[] { @"ProcedureNote", @"eCR_RR_combined_3_1.xml" },
                new object[] { @"ProgressNote", @"eCR_RR_combined_3_1.xml" },
                new object[] { @"ReferralNote", @"eCR_RR_combined_3_1.xml" },
                new object[] { @"TransferSummary", @"eCR_RR_combined_3_1.xml" },

                new object[] { @"ConsultationNote", @"eCR_RR_combined_special_chars.xml" },
                new object[] { @"DischargeSummary", @"eCR_RR_combined_special_chars.xml" },
                new object[] { @"HistoryandPhysical", @"eCR_RR_combined_special_chars.xml" },
                new object[] { @"OperativeNote", @"eCR_RR_combined_special_chars.xml" },
                new object[] { @"ProcedureNote", @"eCR_RR_combined_special_chars.xml" },
                new object[] { @"ProgressNote", @"eCR_RR_combined_special_chars.xml" },
                new object[] { @"ReferralNote", @"eCR_RR_combined_special_chars.xml" },
                new object[] { @"TransferSummary", @"eCR_RR_combined_special_chars.xml" },

                new object[] { @"ConsultationNote", @"eicr04152020.xml" },
                new object[] { @"DischargeSummary", @"eicr04152020.xml" },
                new object[] { @"HistoryandPhysical", @"eicr04152020.xml" },
                new object[] { @"OperativeNote", @"eicr04152020.xml" },
                new object[] { @"ProcedureNote", @"eicr04152020.xml" },
                new object[] { @"ProgressNote", @"eicr04152020.xml" },
                new object[] { @"ReferralNote", @"eicr04152020.xml" },
                new object[] { @"TransferSummary", @"eicr04152020.xml" },
            };
            return cases.Select(item => new object[]
            {
                Convert.ToString(item[0]),
                Path.Combine(_ccdaDataFolder, Convert.ToString(item[1])),
                "EICR",
            });
        }

        protected async Task ConvertAndValidatePatientCount(ITemplateProvider templateProvider, string templateName, string samplePath, string rootTemplate)
        {
            var result = JObject.Parse(_ccdaProcessor.Convert(await File.ReadAllTextAsync(samplePath, Encoding.UTF8), rootTemplate, templateName, templateProvider, fileProvider));
            var patients = result.SelectTokens("$.entry[?(@.resource.resourceType == 'Patient')].resource.id");

            Assert.Equal(1, patients?.Count());
        }

        protected async Task ConvertAndValidateReferenceResourceId(ITemplateProvider templateProvider, string templateName, string samplePath, string rootTemplate)
        {
            HashSet<string> referenceResources = new HashSet<string>();
            var result = JObject.Parse(_ccdaProcessor.Convert(await File.ReadAllTextAsync(samplePath, Encoding.UTF8), rootTemplate, templateName, templateProvider, fileProvider));
            var resources = result.SelectTokens("$.entry..resource");

            // Validate resource id uniqueness
            foreach (var resource in resources)
            {
                var resourceId = resource.SelectTokens("$.id").First().ToString();
                var resouceType = resource.SelectTokens("$.resourceType").First().ToString();
                var referenceStr = $"{resouceType}/{resourceId}";
                Assert.DoesNotContain(referenceStr, referenceResources);
                referenceResources.Add(referenceStr);
            }

            // Validate reference resouce id exists
            foreach (var resource in resources)
            {
                RevealReferences(resource, 0, referenceResources);
            }
        }

        protected async Task ConvertAndValidateNonemptyResource(ITemplateProvider templateProvider, string templateName, string samplePath, string rootTemplate)
        {
            var result = JObject.Parse(_ccdaProcessor.Convert(await File.ReadAllTextAsync(samplePath, Encoding.UTF8), rootTemplate, templateName, templateProvider, fileProvider));
            var resources = result.SelectTokens("$.entry..resource");
            foreach (var resource in resources)
            {
                var properties = resource.ToObject<JObject>().Properties();
                var propNames = properties.Select(p => p.Name).ToHashSet();
                Assert.True(propNames?.Count() > 0);
            }
        }

        protected async Task ConvertAndValidateNonidenticalResources(ITemplateProvider templateProvider, string templateName, string samplePath, string rootTemplate)
        {
            var result = JObject.Parse(_ccdaProcessor.Convert(await File.ReadAllTextAsync(samplePath, Encoding.UTF8), rootTemplate, templateName, templateProvider, fileProvider));
            var resourceIds = result.SelectTokens("$.entry..resource.id");
            var uniqueResourceIds = resourceIds.Select(Convert.ToString).Distinct();
            Assert.Equal(uniqueResourceIds.Count(), resourceIds.Count());
        }

        protected async Task ConvertAndValidateParserFunctionality()
        {
            var jsonResult = await Task.FromResult(@"{
                ""resourceType"": ""Observation"",
                ""id"": ""209c8566-dafa-22b6-31f6-e4c00e649c61"",
                ""valueQuantity"": {
                    ""code"": ""mg/dl""
                },
                ""valueRange"": {	
                    ""low"": {	
                        ""value"": ""182""	
                    }
                }
            }");
            try
            {
                var bundle = _fhirParser.Parse<Hl7.Fhir.Model.Observation>(jsonResult);
                Assert.Null(bundle);
            }
            catch (FormatException fe)
            {
                Assert.NotNull(fe);
            }
        }

        protected async Task ConvertAndValidatePassFhirParser(ITemplateProvider templateProvider, string templateName, string samplePath, string rootTemplate)
        {
            var result = JObject.Parse(_ccdaProcessor.Convert(await File.ReadAllTextAsync(samplePath, Encoding.UTF8), rootTemplate, templateName, templateProvider, fileProvider));
            var jsonResult = JsonConvert.SerializeObject(result, Formatting.Indented);
            try
            {
                var bundle = _fhirParser.Parse<Hl7.Fhir.Model.Bundle>(jsonResult);
                Assert.NotNull(bundle);
            }
            catch (FormatException fe)
            {
                Assert.Null(fe);
            }
        }

        private void RevealReferences(JToken resource, int level, HashSet<string> referenceResources)
        {
            Assert.True(level < _maxRevealDepth, "Reveal depth shouldn't exceed limit.");
            switch (resource)
            {
                case JArray array:
                    array.ToList().ForEach(sub => RevealReferences(sub, level + 1, referenceResources));
                    break;
                case JObject container:
                    var properties = container.Properties();
                    foreach (var property in properties)
                    {
                        if (property.Value.Children().Count() > 0)
                        {
                            RevealReferences(property.Value, level + 1, referenceResources);
                        }
                        else if (property.Name == "reference")
                        {
                            var s = property.Value.ToString();
                            Assert.Contains(s, referenceResources);
                        }
                    }

                    break;
                case JValue value:
                    break;
                default:
                    Assert.True(false, $"Unexpected token {resource}, type {resource.Type}");
                    break;
            }
        }

        private void RevealObjectValues(string origin, JToken resource, int level)
        {
            Assert.True(level < _maxRevealDepth, "Reveal depth shouldn't exceed limit.");
            switch (resource)
            {
                case JArray array:
                    array.ToList().ForEach(sub => RevealObjectValues(origin, sub, level + 1));
                    break;
                case JObject container:
                    container.Properties().Select(p => p.Name).Where(key => ResourceFilter.NonCompareProperties.All(func => !func(key)))
                        .Select(key => container[key]).ToList().ForEach(sub => RevealObjectValues(origin, sub, level + 1));
                    break;
                case JValue value:
                    if (ResourceFilter.NonCompareValues.All(func => !func(value.ToString())))
                    {
                        Assert.Contains(value.ToString().Trim(), origin);
                    }

                    break;
                default:
                    Assert.True(false, $"Unexpected token {resource}, type {resource.Type}");
                    break;
            }
        }

        protected static class ResourceFilter
        {
            private static readonly HashSet<string> _explicitProps = new HashSet<string>
            {
                "resourceType", "type", "fullUrl", "id", "method", "url", "reference", "system",
                "code", "display", "gender", "use", "preferred", "status", "mode", "div", "valueString", "valueCode",
                "text", "endpoint", "value", "category", "type", "criticality", "priority", "severity", "description",
                "intent", "docStatus", "contentType", "authorString", "unit", "outcome",
            };

            private static readonly HashSet<string> _explicitValues = new HashSet<string>
            {
                "order",
                "unknown",
                "source",
            };

            public static readonly List<Func<string, bool>> NonCompareProperties = new List<Func<string, bool>>
            {
                // Exlude all the properties whose value is written in mapping tables explicitly or peculiar to FHIR
                _explicitProps.Contains,
            };

            public static readonly List<Func<string, bool>> NonCompareValues = new List<Func<string, bool>>
            {
                // Exlude all the explicit values written in mapping tables and values peculiar to FHIR
                _explicitValues.Contains,

                // Exclude datetime and boolean values because the format is transformed differently
                (string input) => DateTime.TryParse(input, out _),
                (string input) => bool.TryParse(input, out _),
            };
        }
    }
}