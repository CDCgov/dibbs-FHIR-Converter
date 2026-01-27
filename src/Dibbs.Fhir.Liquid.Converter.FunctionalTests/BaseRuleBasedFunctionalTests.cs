// // -------------------------------------------------------------------------------------------------
// // Copyright (c) Microsoft Corporation. All rights reserved.
// // Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// // -------------------------------------------------------------------------------------------------

// using System;
// using System.Collections.Generic;
// using System.Diagnostics;
// using System.IO;
// using System.Linq;
// using System.Text;
// using System.Text.RegularExpressions;
// using System.Threading.Tasks;
// using Hl7.Fhir.Serialization;
// using Dibbs.Fhir.Liquid.Converter.Models;
// using Dibbs.Fhir.Liquid.Converter.Processors;
// using Newtonsoft.Json;
// using Newtonsoft.Json.Linq;
// using Xunit;
// using Xunit.Abstractions;

// namespace Dibbs.Fhir.Liquid.Converter.FunctionalTests
// {
//     public class BaseRuleBasedFunctionalTests
//     {

//         private static readonly int _maxRevealDepth = 1 << 7;
//         private static readonly bool _skipValidator = true;

//         private readonly ITestOutputHelper _output;

//         public BaseRuleBasedFunctionalTests(ITestOutputHelper output)
//         {
//             this._output = output;

//             _ccdaProcessor = new CcdaProcessor(FhirConverterLogging.CreateLogger<CcdaProcessor>());
//         }

//         protected async Task ConvertAndValidatePatientCount(ITemplateProvider templateProvider, string templateName, string samplePath, DataType dataType)
//         {
//             var result = await ConvertData(templateProvider, templateName, samplePath, dataType);
//             var patients = result.SelectTokens("$.entry[?(@.resource.resourceType == 'Patient')].resource.id");

//             if (ResourceFilter.NonPatientTemplates.All(func => func(templateName)))
//             {
//                 Assert.Equal(0, patients?.Count());
//             }
//             else if (ResourceFilter.MultiplePatientTemplates.All(func => func(templateName)))
//             {
//                 Assert.Equal(2, patients?.Count());
//             }
//             else
//             {
//                 Assert.Equal(1, patients?.Count());
//             }
//         }

//         protected async Task ConvertAndValidateReferenceResourceId(ITemplateProvider templateProvider, string templateName, string samplePath, DataType dataType)
//         {
//             HashSet<string> referenceResources = new HashSet<string>();
//             var result = await ConvertData(templateProvider, templateName, samplePath, dataType);
//             var resources = result.SelectTokens("$.entry..resource");

//             // Validate resource id uniqueness
//             foreach (var resource in resources)
//             {
//                 var resourceId = resource.SelectTokens("$.id").First().ToString();
//                 var resouceType = resource.SelectTokens("$.resourceType").First().ToString();
//                 var referenceStr = $"{resouceType}/{resourceId}";
//                 Assert.DoesNotContain(referenceStr, referenceResources);
//                 referenceResources.Add(referenceStr);
//             }

//             // Validate reference resouce id exists
//             foreach (var resource in resources)
//             {
//                 RevealReferences(resource, 0, referenceResources);
//             }
//         }

//         protected async Task ConvertAndValidateNonemptyResource(ITemplateProvider templateProvider, string templateName, string samplePath, DataType dataType)
//         {
//             var result = await ConvertData(templateProvider, templateName, samplePath, dataType);
//             var resources = result.SelectTokens("$.entry..resource");
//             foreach (var resource in resources)
//             {
//                 var properties = resource.ToObject<JObject>().Properties();
//                 var propNames = properties.Select(p => p.Name).ToHashSet();
//                 Assert.True(propNames?.Count() > 0);
//             }
//         }

//         protected async Task ConvertAndValidateNonidenticalResources(ITemplateProvider templateProvider, string templateName, string samplePath, DataType dataType)
//         {
//             var result = await ConvertData(templateProvider, templateName, samplePath, dataType);
//             var resourceIds = result.SelectTokens("$.entry..resource.id");
//             var uniqueResourceIds = resourceIds.Select(Convert.ToString).Distinct();
//             Assert.Equal(uniqueResourceIds.Count(), resourceIds.Count());
//         }

//         protected async Task ConvertAndValidateValuesRevealInOrigin(ITemplateProvider templateProvider, string templateName, string samplePath, DataType dataType)
//         {
//             var sampleContent = await File.ReadAllTextAsync(samplePath, Encoding.UTF8);
//             var result = await ConvertData(templateProvider, templateName, samplePath, dataType);
//             RevealObjectValues(sampleContent, result, 0);
//         }

//         protected async Task ConvertAndValidatePassOfficialValidator(ITemplateProvider templateProvider, string templateName, string samplePath, DataType dataType)
//         {
//             if (_skipValidator)
//             {
//                 return;
//             }

//             (bool javaStatus, string javaMessage) = await ExecuteCommand("-version");
//             Assert.True(javaStatus, javaMessage);

//             var result = await ConvertData(templateProvider, templateName, samplePath, dataType);
//             var resultFolder = Path.GetFullPath(Path.Combine(@"AppData", "Temp"));
//             var resultPath = Path.Combine(resultFolder, $"{Guid.NewGuid().ToString("N")}.json");
//             if (!Directory.Exists(resultFolder))
//             {
//                 Directory.CreateDirectory(resultFolder);
//             }

//             await File.WriteAllTextAsync(resultPath, JsonConvert.SerializeObject(result, Formatting.Indented), Encoding.UTF8);

//             var validatorPath = Path.GetFullPath(Path.Combine(@"ValidatorLib", "validator_cli.jar"));
//             var specPath = Path.GetFullPath(Path.Combine(@"ValidatorLib", "hl7.fhir.r4.core-4.0.1.tgz"));
//             var command = $"-jar {validatorPath} {resultPath} -version 4.0.1 -ig {specPath} -tx n/a";
//             (bool status, string message) = await ExecuteCommand(command);
//             if (!status)
//             {
//                 Assert.False(status, "Currently the templates are still under development. By default we turn off this validator.");
//                 _output.WriteLine(message);
//             }
//             else
//             {
//                 Assert.True(status);
//             }

//             Directory.Delete(resultFolder, true);
//         }

//         protected async Task ConvertAndValidateParserFunctionality()
//         {
//             var jsonResult = await Task.FromResult(@"{
//                 ""resourceType"": ""Observation"",
//                 ""id"": ""209c8566-dafa-22b6-31f6-e4c00e649c61"",
//                 ""valueQuantity"": {
//                     ""code"": ""mg/dl""
//                 },
//                 ""valueRange"": {	
//                     ""low"": {	
//                         ""value"": ""182""	
//                     }
//                 }
//             }");
//             try
//             {
//                 var bundle = _fhirParser.Parse<Hl7.Fhir.Model.Observation>(jsonResult);
//                 Assert.Null(bundle);
//             }
//             catch (FormatException fe)
//             {
//                 Assert.NotNull(fe);
//             }
//         }

//         protected async Task ConvertAndValidatePassFhirParser(ITemplateProvider templateProvider, string templateName, string samplePath, DataType dataType)
//         {
//             var result = await ConvertData(templateProvider, templateName, samplePath, dataType);
//             var jsonResult = JsonConvert.SerializeObject(result, Formatting.Indented);
//             try
//             {
//                 var bundle = _fhirParser.Parse<Hl7.Fhir.Model.Bundle>(jsonResult);
//                 Assert.NotNull(bundle);
//             }
//             catch (FormatException fe)
//             {
//                 Assert.Null(fe);
//             }
//         }

//         private void RevealReferences(JToken resource, int level, HashSet<string> referenceResources)
//         {
//             Assert.True(level < _maxRevealDepth, "Reveal depth shouldn't exceed limit.");
//             switch (resource)
//             {
//                 case JArray array:
//                     array.ToList().ForEach(sub => RevealReferences(sub, level + 1, referenceResources));
//                     break;
//                 case JObject container:
//                     var properties = container.Properties();
//                     foreach (var property in properties)
//                     {
//                         if (property.Value.Children().Count() > 0)
//                         {
//                             RevealReferences(property.Value, level + 1, referenceResources);
//                         }
//                         else if (property.Name == "reference")
//                         {
//                             var s = property.Value.ToString();
//                             Assert.Contains(s, referenceResources);
//                         }
//                     }

//                     break;
//                 case JValue value:
//                     break;
//                 default:
//                     Assert.True(false, $"Unexpected token {resource}, type {resource.Type}");
//                     break;
//             }
//         }

//         private async Task<JObject> ConvertData(ITemplateProvider templateProvider, string templateName, string samplePath, DataType dataType)
//         {
//             switch (dataType)
//             {
//                 case DataType.Hl7v2:
//                     return JObject.Parse(_hl7v2Processor.Convert(await File.ReadAllTextAsync(samplePath, Encoding.UTF8), templateName, templateProvider));
//                 case DataType.Ccda:
//                     return JObject.Parse(_ccdaProcessor.Convert(await File.ReadAllTextAsync(samplePath, Encoding.UTF8), templateName, templateProvider));
//                 default:
//                     return null;
//             }
//         }

//         private void RevealObjectValues(string origin, JToken resource, int level)
//         {
//             Assert.True(level < _maxRevealDepth, "Reveal depth shouldn't exceed limit.");
//             switch (resource)
//             {
//                 case JArray array:
//                     array.ToList().ForEach(sub => RevealObjectValues(origin, sub, level + 1));
//                     break;
//                 case JObject container:
//                     container.Properties().Select(p => p.Name).Where(key => ResourceFilter.NonCompareProperties.All(func => !func(key)))
//                         .Select(key => container[key]).ToList().ForEach(sub => RevealObjectValues(origin, sub, level + 1));
//                     break;
//                 case JValue value:
//                     if (ResourceFilter.NonCompareValues.All(func => !func(value.ToString())))
//                     {
//                         Assert.Contains(value.ToString().Trim(), origin);
//                     }

//                     break;
//                 default:
//                     Assert.True(false, $"Unexpected token {resource}, type {resource.Type}");
//                     break;
//             }
//         }

//         private async Task<(bool status, string message)> ExecuteCommand(string command)
//         {
//             var rawMessage = string.Empty;
//             var messages = new List<string>();
//             var lineSplitter = new Regex(@"\r|\n|\r\n");

//             var process = new Process
//             {
//                 StartInfo = new ProcessStartInfo
//                 {
//                     FileName = "java",
//                     Arguments = command,
//                     RedirectStandardOutput = true,
//                     RedirectStandardError = true,
//                     CreateNoWindow = true,
//                 },
//             };

//             try
//             {
//                 process.Start();
//                 rawMessage = await process.StandardOutput.ReadToEndAsync();
//                 process.WaitForExit();
//             }
//             catch (Exception e)
//             {
//                 rawMessage = e.Message;
//             }

//             var lines = lineSplitter.Split(rawMessage ?? string.Empty).Select(line => line.Trim())
//                 .Where(line => line.StartsWith("Error"));
//             messages.AddRange(lines);

//             return (process.ExitCode == 0, string.Join(Environment.NewLine, messages));
//         }

//         protected static class ResourceFilter
//         {
//             private static readonly HashSet<string> _explicitProps = new HashSet<string>
//             {
//                 "resourceType", "type", "fullUrl", "id", "method", "url", "reference", "system",
//                 "code", "display", "gender", "use", "preferred", "status", "mode", "div", "valueString", "valueCode",
//                 "text", "endpoint", "value", "category", "type", "criticality", "priority", "severity", "description",
//                 "intent", "docStatus", "contentType", "authorString", "unit", "outcome",
//             };

//             private static readonly HashSet<string> _explicitValues = new HashSet<string>
//             {
//                 "order",
//                 "unknown",
//                 "source",
//             };

//             private static readonly List<string> _noPatientTemplate = new List<string>
//             {
//                 "ADT_A40", "ADT_A41", "ADT_A45", "ADT_A47",
//             };

//             private static readonly List<string> _multiplePatientTemplate = new List<string>
//             {
//                 "BAR_P02",
//             };

//             public static readonly List<Func<string, bool>> NonCompareProperties = new List<Func<string, bool>>
//             {
//                 // Exlude all the properties whose value is written in mapping tables explicitly or peculiar to FHIR
//                 _explicitProps.Contains,
//             };

//             public static readonly List<Func<string, bool>> NonCompareValues = new List<Func<string, bool>>
//             {
//                 // Exlude all the explicit values written in mapping tables and values peculiar to FHIR
//                 _explicitValues.Contains,

//                 // Exclude datetime and boolean values because the format is transformed differently
//                 (string input) => DateTime.TryParse(input, out _),
//                 (string input) => bool.TryParse(input, out _),
//             };

//             public static readonly List<Func<string, bool>> NonPatientTemplates = new List<Func<string, bool>>
//             {
//                 // Templates that don't contain patient resource
//                 _noPatientTemplate.Contains,
//             };

//             public static readonly List<Func<string, bool>> MultiplePatientTemplates = new List<Func<string, bool>>
//             {
//                 // Templates that contain multiple patient resources
//                 _multiplePatientTemplate.Contains,
//             };
//         }
//     }
// }