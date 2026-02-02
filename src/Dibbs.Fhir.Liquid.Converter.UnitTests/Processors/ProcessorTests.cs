// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using Dibbs.Fhir.Liquid.Converter.Exceptions;
using Dibbs.Fhir.Liquid.Converter.Models;
using Dibbs.Fhir.Liquid.Converter.Processors;
using Dibbs.Fhir.Liquid.Converter.Utilities;
using Fluid;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

// TODO: The tests of templates that include other templates will not work unless we mock the IFileProvider that Fluid requires for imports
// at that point we are just testing that Fluid knows how to use its file provider
// We should rework these tests to test the CcdaProcessor without having to mock Fluid's file provider
namespace Dibbs.Fhir.Liquid.Converter.UnitTests.Processors
{
    public class ProcessorTests
    {
        private static readonly string _ecrTestData;
        private static readonly CcdaProcessor _ccdaProcessor;
        private static readonly FluidParser _parser;

        static ProcessorTests()
        {
            _ecrTestData = File.ReadAllText(Path.Join(TestConstants.SampleDataDirectory, "eCR", "eCR_EveEverywoman.xml"));
            _ccdaProcessor = new CcdaProcessor(FhirConverterLogging.CreateLogger<CcdaProcessor>());
            _parser = new FluidParser();
        }

        public static IEnumerable<object[]> GetValidInputsWithTemplateDirectory()
        {
            yield return new object[] { _ccdaProcessor, new TemplateProvider(TestConstants.ECRTemplateDirectory), _ecrTestData, "EICR" };
        }

        public static IEnumerable<object[]> GetValidInputsWithTemplateCollection()
        {
            var templateCollection = new List<Dictionary<string, IFluidTemplate>>
            {
                new Dictionary<string, IFluidTemplate>
                {
                    { "TemplateName", _parser.Parse(@"{""a"":""b""}") },
                },
            };

            yield return new object[] { _ccdaProcessor, new TemplateProvider(templateCollection), _ecrTestData };
        }

        public static IEnumerable<object[]> GetMockDefaultTemplateCollection()
        {
            var rootTemplate = @"{% include 'Sub/Template1' -%}";
            var ecrSubTemplate = @"{""eCR"":""subtemplate1""}";

            var templateCollection = new List<Dictionary<string, IFluidTemplate>>
            {
                new Dictionary<string, IFluidTemplate>
                {
                    { "eCR/Template1", _parser.Parse(rootTemplate) },
                    { "eCR/Sub/Template1", _parser.Parse(ecrSubTemplate) },
                },
            };

            yield return new object[] { _ccdaProcessor, new TemplateProvider(templateCollection, isDefaultTemplateProvider: true), _ecrTestData, ecrSubTemplate };
        }

        public static IEnumerable<object[]> GetNestedTemplateCollection()
        {
            var rootTemplate = @"{% include 'Sub/Template1' -%}";
            var subTemplate = @"{""root"":""subtemplate1""}";
            var folder1SubTemplate = @"{""Folder1"":""subtemplate1""}";
            var folder2SubTemplate = @"{""Folder2"":""subtemplate1""}";

            var templateCollection = new List<Dictionary<string, IFluidTemplate>>
            {
                new Dictionary<string, IFluidTemplate>
                {
                    { "Template1", _parser.Parse(rootTemplate) },
                    { "Sub/Template1", _parser.Parse(subTemplate) },
                    { "Folder1/Template1", _parser.Parse(rootTemplate) },
                    { "Folder1/Sub/Template1", _parser.Parse(folder1SubTemplate) },
                    { "Folder2/Template1", _parser.Parse(rootTemplate) },
                    { "Folder2/Sub/Template1", _parser.Parse(folder2SubTemplate) },
                },
            };

            yield return new object[] { _ccdaProcessor, new TemplateProvider(templateCollection), _ecrTestData, subTemplate, folder1SubTemplate, folder2SubTemplate };
        }


        public static IEnumerable<object[]> GetValidInputsWithLargeForLoop()
        {
            yield return new object[]
            {
                _ccdaProcessor,
                new TemplateProvider(TestConstants.TestTemplateDirectory),
                _ecrTestData,
            };
        }

        public static IEnumerable<object[]> GetValidInputsWithNestingTooDeep()
        {
            yield return new object[]
            {
                _ccdaProcessor,
                new TemplateProvider(TestConstants.TestTemplateDirectory),
                _ecrTestData,
            };
        }

        [Theory]
        [MemberData(nameof(GetValidInputsWithTemplateDirectory))]
        public void GivenAValidTemplateDirectory_WhenConvert_CorrectResultShouldBeReturned(IFhirConverter processor, ITemplateProvider templateProvider, string data, string rootTemplate)
        {
            var result = processor.Convert(data, rootTemplate, TemplateUtility.TemplateDirectory, templateProvider);
            Assert.True(result.Length > 0);
        }

        [Theory]
        [MemberData(nameof(GetValidInputsWithTemplateCollection))]
        public void GivenAValidTemplateCollection_WhenConvert_CorrectResultShouldBeReturned(IFhirConverter processor, ITemplateProvider templateProvider, string data)
        {
            var result = processor.Convert(data, "TemplateName", TemplateUtility.TemplateDirectory, templateProvider);
            Assert.True(result.Length > 0);
        }

        [Theory]
        [MemberData(nameof(GetMockDefaultTemplateCollection))]
        public void GivenDefaultTemplateCollection_WhenConvert_CorrectResultShouldBeReturned(IFhirConverter processor, ITemplateProvider templateProvider, string data, string expectedTemplate)
        {
            var result = processor.Convert(data, "Template1", TemplateUtility.TemplateDirectory, templateProvider);
            Assert.Equal(expectedTemplate, Regex.Replace(result, @"\s", string.Empty));

            result = processor.Convert(data, "Sub/Template1", TemplateUtility.TemplateDirectory, templateProvider);
            Assert.Equal(expectedTemplate, Regex.Replace(result, @"\s", string.Empty));

            var exception = Assert.Throws<RenderException>(() => processor.Convert(data, "NonExistentTemplateName", TemplateUtility.TemplateDirectory, templateProvider));
            Assert.Equal(FhirConverterErrorCode.TemplateNotFound, exception.FhirConverterErrorCode);
        }

        [Theory]
        [MemberData(nameof(GetNestedTemplateCollection))]
        public void GivenNestedTemplateCollection_WhenConvert_CorrectResultShouldBeReturned(IFhirConverter processor, ITemplateProvider templateProvider, string data, string expectedSubTemplate, string expectedFolder1SubTemplate, string expectedFolder2SubTemplate)
        {
            var result = processor.Convert(data, "Template1", TemplateUtility.TemplateDirectory, templateProvider);
            Assert.Equal(expectedSubTemplate, Regex.Replace(result, @"\s", string.Empty));

            result = processor.Convert(data, "Folder1/Template1", TemplateUtility.TemplateDirectory, templateProvider);
            Assert.Equal(expectedFolder1SubTemplate, Regex.Replace(result, @"\s", string.Empty));

            result = processor.Convert(data, "Folder2/Template1", TemplateUtility.TemplateDirectory, templateProvider);
            Assert.Equal(expectedFolder2SubTemplate, Regex.Replace(result, @"\s", string.Empty));

            result = processor.Convert(data, "Folder2/Sub/Template1", TemplateUtility.TemplateDirectory, templateProvider);
            Assert.Equal(expectedFolder2SubTemplate, Regex.Replace(result, @"\s", string.Empty));

            var exception = Assert.Throws<RenderException>(() => processor.Convert(data, "NonExistentTemplateName", TemplateUtility.TemplateDirectory, templateProvider));
            Assert.Equal(FhirConverterErrorCode.TemplateNotFound, exception.FhirConverterErrorCode);
        }

        [Theory]
        [MemberData(nameof(GetValidInputsWithTemplateCollection))]
        public void GivenInvalidTemplateProviderOrName_WhenConvert_ExceptionsShouldBeThrown(IFhirConverter processor, ITemplateProvider templateProvider, string data)
        {
            // Null, empty or nonexistent root template
            var exception = Assert.Throws<RenderException>(() => processor.Convert(data, null, TemplateUtility.TemplateDirectory, templateProvider));
            Assert.Equal(FhirConverterErrorCode.NullOrEmptyRootTemplate, exception.FhirConverterErrorCode);

            exception = Assert.Throws<RenderException>(() => processor.Convert(data, string.Empty, TemplateUtility.TemplateDirectory, templateProvider));
            Assert.Equal(FhirConverterErrorCode.NullOrEmptyRootTemplate, exception.FhirConverterErrorCode);

            exception = Assert.Throws<RenderException>(() => processor.Convert(data, "NonExistentTemplateName", TemplateUtility.TemplateDirectory, templateProvider));
            Assert.Equal(FhirConverterErrorCode.TemplateNotFound, exception.FhirConverterErrorCode);

            // Null TemplateProvider
            exception = Assert.Throws<RenderException>(() => processor.Convert(data, "TemplateName", TemplateUtility.TemplateDirectory, null));
            Assert.Equal(FhirConverterErrorCode.NullTemplateProvider, exception.FhirConverterErrorCode);
        }

        [Theory]
        [MemberData(nameof(GetValidInputsWithLargeForLoop))]
        public void GivenTemplateWithLargeForLoop_WhenConvert_ExceptionShouldBeThrown(IFhirConverter processor, ITemplateProvider templateProvider, string data)
        {
            var exception = Assert.Throws<RenderException>(() => processor.Convert(data, "LargeForLoopTemplate", TemplateUtility.TemplateDirectory, templateProvider));
            Assert.Contains("Render Error - Maximum number of iterations 100000 exceeded", exception.Message);
        }

        [Theory]
        [MemberData(nameof(GetValidInputsWithLargeForLoop))]
        public void GivenTemplateWithNestedForLoop_WhenConvert_CorrectResultShouldBeReturned(IFhirConverter processor, ITemplateProvider templateProvider, string data)
        {
            var result = processor.Convert(data, "NestedForLoopTemplate", TemplateUtility.TemplateDirectory, templateProvider);
            Assert.True(result.Length > 0);
        }

        [Theory]
        [MemberData(nameof(GetValidInputsWithNestingTooDeep))]
        public void GivenTemplateWithNestingTooDeep_WhenConvert_ExceptionShouldBeThrown(IFhirConverter processor, ITemplateProvider templateProvider, string data)
        {
            var exception = Assert.Throws<RenderException>(() => processor.Convert(data, "NestingTooDeepTemplate", TemplateUtility.TemplateDirectory, templateProvider));
            Assert.Contains("Nesting too deep", exception.Message);

            exception = Assert.Throws<RenderException>(() => processor.Convert(data, "NestingTooDeepDiffTemplate", TemplateUtility.TemplateDirectory, templateProvider));
            Assert.Contains("Nesting too deep", exception.Message);
        }
    }
}
