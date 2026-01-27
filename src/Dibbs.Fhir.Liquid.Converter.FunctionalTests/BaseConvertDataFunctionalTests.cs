// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Firely.Fhir.Packages;
using Firely.Fhir.Validation;
using Firely.Fhir.Validation.Compilation;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Specification.Terminology;
using Hl7.Fhir.Support;
using Hl7.Fhir.Utility;
using Hl7.Fhir.Validation;
using Dibbs.Fhir.Liquid.Converter.Models;
using Dibbs.Fhir.Liquid.Converter.Processors;
using Dibbs.Fhir.Liquid.Converter.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Dibbs.Fhir.Liquid.Converter.FunctionalTests
{
    public class BaseConvertDataFunctionalTests
    {
        public static IEnumerable<object[]> GetDataForEcr()
        {
            var data = new List<string[]>
            {
                // Array has the following fields:
                // [
                //   1. Root template,
                //   2. ecr file,
                //   3. expected fhir file,
                //   4. whether the file should fail at parsing or validation when testing if valid (if it is fully valid, "validation" is what should be there)
                //   5. The number of expected failures at the step in (4) for the numFailures parameter
                // ]
                new[] { @"EICR", @"eCR_full.xml", @"eCR_full-expected.json", "validation", "3" },
                new[] { @"EICR", @"eCR_RR_combined_3_1.xml", @"eCR_RR_combined_3_1-expected.json", "validation", "9" },
                new[] { @"EICR", @"eCR_EveEverywoman.xml", @"eCR_EveEverywoman-expected.json", "validation", "31" },
                new[] { @"EICR", @"eicr04152020.xml", @"eicr04152020-expected.json", "validation", "15" },
                new[] { @"EICR", @"CDAR2_IG_PHCASERPT_R2_D2_SAMPLE.xml", @"CDAR2_IG_PHCASERPT_R2_D2_SAMPLE-expected.json", "validation", "15" },
            };
            return data.Select(item => new[]
            {
                item[0],
                Path.Join(Constants.SampleDataDirectory, "eCR", item[1]),
                Path.Join(Constants.ExpectedDataFolder, "eCR", item[0], item[2]),
                item[3],
                item[4],
            });
        }

        protected void ConvertCCDAMessageAndValidateExpectedResponse(ITemplateProvider templateProvider, string rootTemplate, string inputFile, string expectedFile)
        {
            var ccdaProcessor = new CcdaProcessor(FhirConverterLogging.CreateLogger<CcdaProcessor>());
            var inputContent = File.ReadAllText(inputFile);
            var actualContent = ccdaProcessor.Convert(inputContent, rootTemplate, TemplateUtility.TemplateDirectory, templateProvider);

            var updateSnapshot = Environment.GetEnvironmentVariable("UPDATE_SNAPSHOT") ?? "false";
            if (true)
            {
                File.WriteAllText(expectedFile, actualContent);
            }

            var expectedContent = File.ReadAllText(expectedFile);

            var expectedObject = JObject.Parse(expectedContent);
            var actualObject = JObject.Parse(actualContent);

            // Remove DocumentReference, where date is different every time conversion is run and gzip result is OS dependent
            if (expectedObject["entry"]?.Last()["resource"]["resourceType"].ToString() == "DocumentReference")
            {
                expectedObject["entry"]?.Last()?.Remove();
                actualObject["entry"]?.Last()?.Remove();
            }

            var diff = DiffHelper.FindDiff(actualObject, expectedObject);
            File.WriteAllText("tempFile.txt", diff.ToString());
            if (diff.HasValues)
            {
                Console.WriteLine(diff);
            }

            Assert.True(JToken.DeepEquals(expectedObject, actualObject));
        }

        protected void ValidateConvertCCDAMessageIsValidFHIR(ITemplateProvider templateProvider, string rootTemplate, string inputFile, string validationFailureStep, int numFailures)
        {
            System.Console.Out.WriteLine("#####################################################################################################");
            System.Console.Out.WriteLine(inputFile);
            System.Console.Out.WriteLine("-----------------------------------------------------------------------------------------------------");
            var validateFhir = Environment.GetEnvironmentVariable("VALIDATE_FHIR") ?? "false";
            if (validateFhir.Trim() == "false") return;

            var ccdaProcessor = new CcdaProcessor(FhirConverterLogging.CreateLogger<CcdaProcessor>());
            var inputContent = File.ReadAllText(inputFile);
            var actualContent = ccdaProcessor.Convert(inputContent, rootTemplate, TemplateUtility.TemplateDirectory, templateProvider);

            var fhirJsonPocoDeserializerSettings = new FhirJsonPocoDeserializerSettings()
            {
                ValidateOnFailedParse = true
            };
            var serializerOptions = new JsonSerializerOptions()
                .ForFhir(ModelInfo.ModelInspector, fhirJsonPocoDeserializerSettings)
                .Ignoring([CodedValidationException.DATETIME_LITERAL_INVALID_CODE]);
            // Ignoring datetime formatting because FHIR does not like datetimes with times without a time zone.

            try
            {
                var poco = System.Text.Json.JsonSerializer.Deserialize<Bundle>(
                    actualContent,
                    serializerOptions
                );
                var ecrSource = new FhirPackageSource(
                    ModelInfo.ModelInspector,
                    "https://packages2.fhir.org/packages",
                    new string[] { "hl7.fhir.us.ecr@2.1.2", }
                );
                var coreSource = FhirPackageSource.CreateCorePackageSource(
                    ModelInfo.ModelInspector,
                    FhirRelease.R4,
                    "https://packages2.fhir.org/packages"
                );

                var profileSource = new CachedResolver(ecrSource);
                var loincClient = new FhirClient("https://fhir.loinc.org");
                loincClient.RequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue(
                        "Basic",
                        "am55Z2FhcmQ6M1hTQCFld2NBQWVMc1pN"
                    );
                var loincTerminologyService = new ExternalTerminologyService(loincClient);
                var terminologyService = new LocalTerminologyService(profileSource);
                var baseTermSer = LocalTerminologyService.CreateDefaultForCore(coreSource);
                var mulTermSer = new MultiTerminologyService(
                    terminologyService,
                    baseTermSer,
                    loincTerminologyService
                );

                var validator = new Validator(profileSource, mulTermSer);
                var result = validator.Validate(poco);
                var outcomeText = result.ToString();
                var numFailed = result.Issue.Count();

                if ("validation" == validationFailureStep && numFailures == numFailed)
                {
                    Console.WriteLine(result.ToString());
                }

                Assert.Equal("validation", validationFailureStep);
                Assert.True(numFailures == numFailed, $"!!Validation failed!!\nExpected {numFailures}, but got {numFailed}\n{outcomeText}");
            }
            catch (DeserializationFailedException e)
            {

                var errors = e.Message.Replace(") (", ")\n(");
                var numFailed = errors.Count(f => f == '\n') + 1;

                if ("parsing" == validationFailureStep && numFailures == numFailed)
                {
                    Console.WriteLine(errors);
                }

                Assert.Equal("parsing", validationFailureStep);
                Assert.True(numFailures == numFailed, $"!!Parsing failed!!\nExpected {numFailures}, but got {numFailed}\n{errors}");
                return;
            }
        }
    }
}
