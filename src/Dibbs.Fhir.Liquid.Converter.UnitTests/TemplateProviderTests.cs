// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using Dibbs.Fhir.Liquid.Converter.Exceptions;
using Dibbs.Fhir.Liquid.Converter.Models;
using Fluid;
using Xunit;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests
{
    public class TemplateProviderTests
    {
        private readonly FluidParser parser;
        public TemplateProviderTests()
        {
            parser = new FluidParser();
        }

        public static IEnumerable<object[]> GetValidTemplateProvider()
        {
            yield return new object[] { new TemplateProvider(TestConstants.ECRTemplateDirectory),  "EICR" };
        }

        public static IEnumerable<object[]> GetInvalidTemplateDirectory()
        {
            yield return new object[] { string.Empty };
            yield return new object[] { Path.Join("a", "b", "c") };
        }

        [Theory]
        [MemberData(nameof(GetValidTemplateProvider))]
        public void GivenAValidTemplateProviderFromLocalFileSystem_WhenGetTemplate_CorrectResultsShouldBeReturned(ITemplateProvider directoryTemplateProvider, string rootTemplate)
        {
            Assert.NotNull(directoryTemplateProvider.GetTemplate(rootTemplate));
        }

        [Fact]
        public void GivenAValidTemplateProviderFromMemoryFileSystem_WhenGetTemplate_CorrectResultsShouldBeReturned()
        {
            var collection = new List<Dictionary<string, IFluidTemplate>>
            {
                new Dictionary<string, IFluidTemplate>
                {
                    { "foo", parser.Parse("bar") },
                },
            };

            var collectionTemplateProvider = new TemplateProvider(collection);
            Assert.NotNull(collectionTemplateProvider.GetTemplate("foo"));
            Assert.False(collectionTemplateProvider.IsDefaultTemplateProvider);
        }

        [Fact]
        public void GivenDefaultTemplateProviderFromMemoryFileSystem_WhenGetTemplate_CorrectResultsShouldBeReturned()
        {
            var collection = new List<Dictionary<string, IFluidTemplate>>
            {
                new Dictionary<string, IFluidTemplate>
                {
                    { "Hl7v2/foo", parser.Parse("bar") },
                },
            };

            var collectionTemplateProvider = new TemplateProvider(collection, isDefaultTemplateProvider: true);
            Assert.NotNull(collectionTemplateProvider.GetTemplate("Hl7v2/foo"));
            Assert.True(collectionTemplateProvider.IsDefaultTemplateProvider);
        }

        [Theory]
        [MemberData(nameof(GetInvalidTemplateDirectory))]
        public void GivenInvalidTemplateDirectory_WhenCreateTemplateProvider_ExceptionShouldBeReturned(string templateDirectory)
        {
            Assert.Throws<TemplateLoadException>(() => new TemplateProvider(templateDirectory));
        }
    }
}