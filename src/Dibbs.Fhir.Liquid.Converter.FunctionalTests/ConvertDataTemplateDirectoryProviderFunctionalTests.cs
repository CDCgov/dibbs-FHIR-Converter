// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using Dibbs.Fhir.Liquid.Converter.Exceptions;
using Dibbs.Fhir.Liquid.Converter.Models;
using Dibbs.Fhir.Liquid.Converter.Processors;
using Dibbs.Fhir.Liquid.Converter.Utilities;
using Fluid;
using Newtonsoft.Json;
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
            var ccdaProcessor = new CcdaProcessor(FhirConverterLogging.CreateLogger<CcdaProcessor>());
            var templateDirectory = Path.Join(Constants.TestDataDirectory, "TimezoneHandling", "Template");

            var inputContent = File.ReadAllText(inputFile);
            var actualContent = ccdaProcessor.Convert(inputContent, "CcdaTestTimezoneTemplate", templateDirectory, new TemplateProvider(templateDirectory));

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

        // TODO: Fix error
        [Fact]
        public void GivenAnInvalidTemplate_WhenConverting_ExceptionsShouldBeThrown()
        {
            var ccdaProcessor = new CcdaProcessor(FhirConverterLogging.CreateLogger<CcdaProcessor>());
            var parser = new FluidParser();
            var templateCollection = new List<Dictionary<string, IFluidTemplate>>
            {
                new Dictionary<string, IFluidTemplate>
                {
                    { "template", parser.Parse("{% include 'template' -%}") },
                },
            };

            var exception = Assert.Throws<RenderException>(() => ccdaProcessor.Convert(@"<ClinicalDocument></ClinicalDocument>", "template", TemplateUtility.TemplateDirectory, new TemplateProvider(templateCollection)));
            Console.WriteLine("#####");
            Console.WriteLine(exception.InnerException.GetType());
            Assert.True(exception.InnerException is RenderException);
        }
    }
}
