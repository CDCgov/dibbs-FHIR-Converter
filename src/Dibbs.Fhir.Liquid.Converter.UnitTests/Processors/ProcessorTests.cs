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
using Dibbs.Fhir.Liquid.Converter.UnitTests.Mocks;
using Dibbs.Fhir.Liquid.Converter.Utilities;
using Fluid;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests.Processors
{
    public class ProcessorTests
    {
        private static readonly string _ecrTestData;
        private static readonly FluidParser _parser;

        static ProcessorTests()
        {
            _ecrTestData = File.ReadAllText(Path.Join(TestConstants.SampleDataDirectory, "eCR", "eCR_EveEverywoman.xml"));
            _parser = new FluidParser();
        }

        private static TemplateOptions GetTemplateOptions ()
        {
            var options = new TemplateOptions
            {
                MaxSteps = 10000000,
            };
            TemplateUtility.AddFilters(options);

            return options;
        }

        public static IEnumerable<object[]> GetValidInputsWithTemplateDirectory()
        {
            var ccdaProcessor = new CcdaProcessor(FhirConverterLogging.CreateLogger<CcdaProcessor>(), TemplateUtility.TemplateOptions);
            yield return new object[] { ccdaProcessor, new TemplateProvider(TestConstants.ECRTemplateDirectory), _ecrTestData, "EICR" };
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

            var ccdaProcessor = new CcdaProcessor(FhirConverterLogging.CreateLogger<CcdaProcessor>(), GetTemplateOptions());
            yield return new object[] { ccdaProcessor, new TemplateProvider(templateCollection), _ecrTestData };
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

            var fileProvider = new MockFileProvider();
            fileProvider.Add("Template1.liquid", rootTemplate);
            fileProvider.Add("Sub/Template1.liquid", ecrSubTemplate);

            var ccdaProcessor = new CcdaProcessor(FhirConverterLogging.CreateLogger<CcdaProcessor>(), GetTemplateOptions());
            yield return new object[] { ccdaProcessor, new TemplateProvider(templateCollection, isDefaultTemplateProvider: true), _ecrTestData, ecrSubTemplate, fileProvider };
        }

        public static IEnumerable<object[]> GetNestedTemplateCollection()
        {
            var rootTemplate = @"{% include 'Sub/Template1' -%}";
            var subTemplate = @"{""root"":""subtemplate1""}";

            var templateCollection = new List<Dictionary<string, IFluidTemplate>>
            {
                new Dictionary<string, IFluidTemplate>
                {
                    { "Template1", _parser.Parse(rootTemplate) },
                    { "Sub/Template1", _parser.Parse(subTemplate) },
                    { "Folder1/Template1", _parser.Parse(rootTemplate) },
                },
            };

            var fileProvider = new MockFileProvider();
            fileProvider.Add("Template1.liquid", rootTemplate);
            fileProvider.Add("Sub/Template1.liquid", subTemplate);
            fileProvider.Add("Folder1/Template1.liquid", rootTemplate);

            var ccdaProcessor = new CcdaProcessor(FhirConverterLogging.CreateLogger<CcdaProcessor>(), GetTemplateOptions());
            yield return new object[] { ccdaProcessor, new TemplateProvider(templateCollection), _ecrTestData, subTemplate, fileProvider };
        }


        public static IEnumerable<object[]> GetValidInputsWithLargeForLoop()
        {
            var ccdaProcessor = new CcdaProcessor(FhirConverterLogging.CreateLogger<CcdaProcessor>(), GetTemplateOptions());

            yield return new object[]
            {
                ccdaProcessor,
                new TemplateProvider(TestConstants.TestTemplateDirectory),
                _ecrTestData,
            };
        }

        public static IEnumerable<object[]> GetValidInputsWithNestingTooDeep()
        {
            var ccdaProcessor = new CcdaProcessor(FhirConverterLogging.CreateLogger<CcdaProcessor>(), GetTemplateOptions());
            yield return new object[]
            {
                ccdaProcessor,
                new TemplateProvider(TestConstants.TestTemplateDirectory),
                _ecrTestData,
            };
        }

        [Theory]
        [MemberData(nameof(GetValidInputsWithTemplateDirectory))]
        public void GivenAValidTemplateDirectory_WhenConvert_CorrectResultShouldBeReturned(IFhirConverter processor, ITemplateProvider templateProvider, string data, string rootTemplate)
        {
            var fileProvider = new PhysicalFileProvider(Path.GetFullPath(TestConstants.ECRTemplateDirectory));
            var result = processor.Convert(data, rootTemplate, TemplateUtility.TemplateDirectory, templateProvider, fileProvider);
            Assert.True(result.Length > 0);
        }
        
        [Theory]
        [MemberData(nameof(GetValidInputsWithTemplateCollection))]
        public void GivenAValidTemplateCollection_WhenConvert_CorrectResultShouldBeReturned(IFhirConverter processor, ITemplateProvider templateProvider, string data)
        {
            var fileProvider = new MockFileProvider();
            var result = processor.Convert(data, "TemplateName", TemplateUtility.TemplateDirectory, templateProvider, fileProvider);
            Assert.True(result.Length > 0);
        }
        
        [Theory]
        [MemberData(nameof(GetMockDefaultTemplateCollection))]
        public void GivenDefaultTemplateCollection_WhenConvert_CorrectResultShouldBeReturned(IFhirConverter processor, ITemplateProvider templateProvider, string data, string expectedTemplate, IFileProvider fileProvider)
        {
            var result = processor.Convert(data, "Template1", TemplateUtility.TemplateDirectory, templateProvider, fileProvider);
            Assert.Equal(expectedTemplate, Regex.Replace(result, @"\s", string.Empty));
        
            result = processor.Convert(data, "Sub/Template1", TemplateUtility.TemplateDirectory, templateProvider, fileProvider);
            Assert.Equal(expectedTemplate, Regex.Replace(result, @"\s", string.Empty));
        
            var exception = Assert.Throws<RenderException>(() => processor.Convert(data, "NonExistentTemplateName", TemplateUtility.TemplateDirectory, templateProvider, fileProvider));
            Assert.Equal(FhirConverterErrorCode.TemplateNotFound, exception.FhirConverterErrorCode);
        }
        
        [Theory]
        [MemberData(nameof(GetNestedTemplateCollection))]
        public void GivenNestedTemplateCollection_WhenConvert_CorrectResultShouldBeReturned(IFhirConverter processor, ITemplateProvider templateProvider, string data, string expectedSubTemplate, IFileProvider fileProvider)
        {
            var result = processor.Convert(data, "Template1", TemplateUtility.TemplateDirectory, templateProvider, fileProvider);
            Assert.Equal(expectedSubTemplate, Regex.Replace(result, @"\s", string.Empty));
        
            // Fluid works differently from DotLiquid. 
            // Make sure your template paths and include tags are always relative to the root path (i.e. data/Templates/eCR).
            result = processor.Convert(data, "Folder1/Template1", TemplateUtility.TemplateDirectory, templateProvider, fileProvider);
            Assert.Equal(expectedSubTemplate, Regex.Replace(result, @"\s", string.Empty)); 
        
            var exception = Assert.Throws<RenderException>(() => processor.Convert(data, "NonExistentTemplateName", TemplateUtility.TemplateDirectory, templateProvider, fileProvider));
            Assert.Equal(FhirConverterErrorCode.TemplateNotFound, exception.FhirConverterErrorCode);
        }
        
        [Theory]
        [MemberData(nameof(GetValidInputsWithTemplateCollection))]
        public void GivenInvalidTemplateProviderOrName_WhenConvert_ExceptionsShouldBeThrown(IFhirConverter processor, ITemplateProvider templateProvider, string data)
        {
            var fileProvider = new MockFileProvider();

            // Null, empty or nonexistent root template
            var exception = Assert.Throws<RenderException>(() => processor.Convert(data, null, TemplateUtility.TemplateDirectory, templateProvider, fileProvider));
            Assert.Equal(FhirConverterErrorCode.NullOrEmptyRootTemplate, exception.FhirConverterErrorCode);
        
            exception = Assert.Throws<RenderException>(() => processor.Convert(data, string.Empty, TemplateUtility.TemplateDirectory, templateProvider, fileProvider));
            Assert.Equal(FhirConverterErrorCode.NullOrEmptyRootTemplate, exception.FhirConverterErrorCode);
        
            exception = Assert.Throws<RenderException>(() => processor.Convert(data, "NonExistentTemplateName", TemplateUtility.TemplateDirectory, templateProvider, fileProvider));
            Assert.Equal(FhirConverterErrorCode.TemplateNotFound, exception.FhirConverterErrorCode);
        
            // Null TemplateProvider
            exception = Assert.Throws<RenderException>(() => processor.Convert(data, "TemplateName", TemplateUtility.TemplateDirectory, null, fileProvider));
            Assert.Equal(FhirConverterErrorCode.NullTemplateProvider, exception.FhirConverterErrorCode);
        }

        [Theory]
        [MemberData(nameof(GetValidInputsWithLargeForLoop))]
        public void GivenTemplateWithLargeForLoop_WhenConvert_ExceptionShouldBeThrown(IFhirConverter processor, ITemplateProvider templateProvider, string data)
        {
            var fileProvider = new PhysicalFileProvider(Path.GetFullPath(TestConstants.TestTemplateDirectory));
            var exception = Assert.Throws<RenderException>(() => processor.Convert(data, "LargeForLoopTemplate", TemplateUtility.TemplateDirectory, templateProvider, fileProvider));
            Assert.Contains("Error happened when rendering templates: The maximum level of recursion has been reached. Your script must have a cyclic include statement.", exception.Message);
        }

        [Theory]
        [MemberData(nameof(GetValidInputsWithLargeForLoop))]
        public void GivenTemplateWithNestedForLoop_WhenConvert_CorrectResultShouldBeReturned(IFhirConverter processor, ITemplateProvider templateProvider, string data)
        {
            // When using DotLiquid, we could have nested for loops where the total number of iterations exceeded the MaxIterations that we defined
            // Fluid's MaxSteps works differently
            // DotLiquid tracks the number of iterations of each for loop separately
            // Fluid increments the number of steps taken when any statement is executed
            var fileProvider = new PhysicalFileProvider(Path.GetFullPath(TestConstants.TestTemplateDirectory));
            var exception = Assert.Throws<RenderException>(() => processor.Convert(data, "NestedForLoopTemplate", TemplateUtility.TemplateDirectory, templateProvider, fileProvider));
            Assert.Equal("Error happened when rendering templates: The maximum level of recursion has been reached. Your script must have a cyclic include statement.", exception.Message);
        }

        [Theory]
        [MemberData(nameof(GetValidInputsWithNestingTooDeep))]
        public void GivenTemplateWithNestingTooDeep_WhenConvert_ExceptionShouldBeThrown(IFhirConverter processor, ITemplateProvider templateProvider, string data)
        {
            var fileProvider = new PhysicalFileProvider(Path.GetFullPath(TestConstants.TestTemplateDirectory));
            var exception = Assert.Throws<RenderException>(() => processor.Convert(data, "NestingTooDeepTemplate", TemplateUtility.TemplateDirectory, templateProvider, fileProvider));
            Assert.Equal("Error happened when rendering templates: The maximum level of recursion has been reached. Your script must have a cyclic include statement.", exception.Message);
        }
    }
}
