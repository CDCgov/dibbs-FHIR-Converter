// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Dibbs.Fhir.Liquid.Converter.Exceptions;
using Dibbs.Fhir.Liquid.Converter.Models;
using Dibbs.Fhir.Liquid.Converter.Utilities;
using Fluid;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Xunit;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests.Utilities
{
    public class TemplateUtilityTests
    {
        [Fact]
        public void GivenValidCcdaTemplateContents_WhenParseTemplates_CorrectResultShouldBeReturned()
        {
            var parsedTemplate = TemplateUtility.ParseTemplate("Resource/_Patient.liquid", "b");
            Assert.Equal("b", parsedTemplate.Render());

            parsedTemplate = TemplateUtility.ParseTemplate("Resource/_Patient.liquid", null);
            Assert.Null(parsedTemplate.Render());
        }

        [Fact]
        public void GivenInvalidCcdaTemplateContents_WhenParseTemplates_ExceptionsShouldBeThrown()
        {
            // Invalid DotLiquid template
            var templates = new Dictionary<string, string> { { "CCD.liquid", "{{" } };
            var exception = Assert.Throws<TemplateLoadException>(() => TemplateUtility.ParseTemplate("CCD.liquid", "{{"));
            Assert.Equal(FhirConverterErrorCode.TemplateSyntaxError, exception.FhirConverterErrorCode);
            Assert.True(exception.InnerException is RenderException);
        }

        [Fact]
        public void GivenNestedTemplates_WhenParseTemplates_CorrectResultShouldBeReturned()
        {
            var parsedTemplate = TemplateUtility.ParseTemplate(@"eCR/Resource\_Encounter.liquid", "c");
            Assert.Equal("c", parsedTemplate.Render());
            
            parsedTemplate = TemplateUtility.ParseTemplate(@"eCR/Resource\_Encounter.liquid", null);
            Assert.Null(parsedTemplate.Render());
        }

        [Theory]
        [InlineData("ADT_A01.liquid", "", "ADT_A01.liquid")]
        [InlineData("ADT_A01.liquid", "Hl7v2", "Hl7v2/ADT_A01.liquid")]
        [InlineData("ADT_A01.liquid", "Hl7v2/Hl7v21", "Hl7v2/Hl7v21/ADT_A01.liquid")]
        [InlineData("Hl7v21/ADT_A01.liquid", "Hl7v2", "Hl7v2/Hl7v21/ADT_A01.liquid")]
        public void GivenRootTemplate_WhenGetFormattedTemplatePath_CorrectResultShouldBeReturned(string templateName, string rootTemplateParentPath, string expectedFormattedTemplatePath)
        {
            Assert.Equal(expectedFormattedTemplatePath, TemplateUtility.GetFormattedTemplatePath(templateName, rootTemplateParentPath));
        }
    }
}
