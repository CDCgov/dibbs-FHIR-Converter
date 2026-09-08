// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Xml.Linq;
using Dibbs.Fhir.Liquid.Converter.Exceptions;
using Dibbs.Fhir.Liquid.Converter.Models;
using Dibbs.Fhir.Liquid.Converter.Processors;
using Dibbs.Fhir.Liquid.Converter.Utilities;
using Dibbs.FhirConverterApi.Processors;
using Fluid;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Dibbs.Fhir.Liquid.Converter.FunctionalTests
{
    public class ConvertDataTemplateDirectoryProviderFunctionalTests : BaseConvertDataFunctionalTests
    {
        private readonly ITestOutputHelper _outputHelper;

        public ConvertDataTemplateDirectoryProviderFunctionalTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public void GivenCcdaMessageForTimezoneTesting_WhenConvert_ExpectedResultShouldBeReturned()
        {
            var inputFile = Path.Combine(Constants.TestDataDirectory, "TimezoneHandling", "Input", "CcdaTestTimezoneInput.ccda");
            var ccdaProcessor = new CcdaProcessor(FhirConverterLogging.CreateLogger<CcdaProcessor>(), TemplateUtility.TemplateOptions);
            var templateDirectory = Path.Join(Constants.TestDataDirectory, "TimezoneHandling", "Template");
            var fileProvider = new PhysicalFileProvider(Path.GetFullPath(TemplateUtility.TemplateDirectory));

            var inputContent = File.ReadAllText(inputFile);
            var actualContent = ccdaProcessor.Convert(inputContent, "CcdaTestTimezoneTemplate", templateDirectory, new TemplateProvider(templateDirectory), fileProvider);

            var actualObject = JsonConvert.DeserializeObject<Dictionary<string, string>>(actualContent);

            Assert.Equal("2001-01", actualObject["datetime1"]);
            Assert.Equal("2001-01-01", actualObject["datetime2"]);
            Assert.Equal("2001-01-01", actualObject["datetime3"]);
            Assert.Equal("2001-11-11T12:00:00", actualObject["datetime4"]);
            Assert.Equal("2001-11-11T12:23:00", actualObject["datetime5"]);
            Assert.Equal("2020-01-01T01:01:01+08:00", actualObject["datetime6"]);
        }

        [Theory]
        [MemberData(nameof(GetDataForEcr))]
        public void GivenEcrDocument_WhenConverting_ExpectedFhirResourceShouldBeReturned(string rootTemplate, string inputFile, string expectedFile, string _validationFailureStep, string _numFailures)
        {
            var templateDirectory = Path.Join(AppDomain.CurrentDomain.BaseDirectory, Constants.TemplateDirectory, "eCR");
            var templateProvider = new TemplateProvider(templateDirectory);

            ConvertCCDAMessageAndValidateExpectedResponse(templateProvider, rootTemplate, inputFile, expectedFile);
        }

        [Fact]
        public void GivenEcrWithEntryReference_WhenConverting_ExtensionTargetsExistingResource()
        {
            const string entryReferenceUrl = "https://github.com/CDCgov/dibbs-FHIR-Converter/StructureDefinition/cda-entry-reference";
            var templateDirectory = Path.Join(AppDomain.CurrentDomain.BaseDirectory, Constants.TemplateDirectory, "eCR");
            var templateProvider = new TemplateProvider(templateDirectory);
            var fileProvider = new PhysicalFileProvider(Path.GetFullPath(TemplateUtility.TemplateDirectory));
            var inputFile = Path.Join(Constants.SampleDataDirectory, "eCR", "eCR_RR_combined_3_1.xml");
            var cdaDocument = EcrProcessor.ResolveReferences(XDocument.Parse(File.ReadAllText(inputFile)));
            var processor = new CcdaProcessor(FhirConverterLogging.CreateLogger<CcdaProcessor>(), TemplateUtility.TemplateOptions);

            var converted = processor.Convert(cdaDocument.ToString(), "EICR", TemplateUtility.TemplateDirectory, templateProvider, fileProvider);
            var bundle = JObject.Parse(converted);
            var resources = bundle["entry"]?.Select(entry => entry["resource"]).OfType<JObject>().ToList() ?? new List<JObject>();
            var entryReferences = resources
                .SelectMany(resource => (resource["extension"] as JArray ?? new JArray())
                    .OfType<JObject>()
                    .Where(extension => extension.Value<string>("url") == entryReferenceUrl)
                    .Select(extension => (Resource: resource, Extension: extension)))
                .ToList();

            var entryReference = Assert.Single(entryReferences);
            Assert.Equal("DiagnosticReport", entryReference.Resource.Value<string>("resourceType"));

            var target = entryReference.Extension["extension"]?
                .OfType<JObject>()
                .Single(part => part.Value<string>("url") == "target")["valueReference"]?["reference"]?
                .Value<string>();

            Assert.False(string.IsNullOrEmpty(target));
            Assert.Contains(resources, resource => target == $"{resource.Value<string>("resourceType")}/{resource.Value<string>("id")}");
        }

        [Theory]
        [MemberData(nameof(GetDataForEcr))]
        public void GivenEcrDocument_WhenConverting_ExpectedFhirResourceShouldBeValid(string rootTemplate, string inputFile, string expectedFile, string validationFailureStep, string numFailures)
        {
            var templateDirectory = Path.Join(AppDomain.CurrentDomain.BaseDirectory, Constants.TemplateDirectory, "eCR");
            var templateProvider = new TemplateProvider(templateDirectory);

            ValidateConvertCCDAMessageIsValidFHIR(
                templateProvider,
                rootTemplate,
                inputFile,
                validationFailureStep,
                Int32.Parse(numFailures)
            );
        }

        [Fact]
        public void GivenAnInvalidTemplate_WhenConverting_ExceptionsShouldBeThrown()
        {
            var templateOptions = new TemplateOptions();
            var ccdaProcessor = new CcdaProcessor(FhirConverterLogging.CreateLogger<CcdaProcessor>(), templateOptions);
            var fileProvider = new PhysicalFileProvider(Path.GetFullPath(Constants.TestTemplatesDirectory));

            var exception = Assert.Throws<RenderException>(() => ccdaProcessor.Convert(@"<ClinicalDocument></ClinicalDocument>", "NestingTooDeepTemplate", TemplateUtility.TemplateDirectory, new TemplateProvider(Constants.TestTemplatesDirectory), fileProvider));
            Assert.True(exception.InnerException is InvalidOperationException);
        }
    }
}
