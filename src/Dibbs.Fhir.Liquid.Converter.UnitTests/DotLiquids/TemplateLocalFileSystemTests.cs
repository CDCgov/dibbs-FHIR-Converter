// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.IO;
using Dibbs.Fhir.Liquid.Converter.Exceptions;
using Dibbs.Fhir.Liquid.Converter.FileSystems;
using Dibbs.Fhir.Liquid.Converter.Models;
using Fluid;
using Newtonsoft.Json.Linq;
using NJsonSchema;
using Xunit;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests.DotLiquids
{
    public class TemplateLocalFileSystemTests
    {
        [Fact]
        public void GivenAValidTemplateDirectory_WhenGetTemplate_CorrectResultsShouldBeReturned()
        {
            var templateLocalFileSystem = new TemplateLocalFileSystem(TestConstants.ECRTemplateDirectory);

            // Template exists
            Assert.NotNull(templateLocalFileSystem.GetTemplate("EICR"));

            // Template does not exist
            Assert.Null(templateLocalFileSystem.GetTemplate("Foo"));
        }

        [Fact]
        public void GivenAValidTemplateDirectory_WhenGetTemplateWithContext_CorrectResultsShouldBeReturned()
        {
            var templateLocalFileSystem = new TemplateLocalFileSystem(TestConstants.ECRTemplateDirectory);
            var context = new TemplateContext(CultureInfo.InvariantCulture);

            // Template exists
            context.SetValue("EICR", "EICR");
            Assert.NotNull(templateLocalFileSystem.GetTemplate(context, "EICR"));

            // Template does not exist
            context.SetValue("Foo", "Foo");
            Assert.Throws<RenderException>(() => templateLocalFileSystem.GetTemplate(context, "Foo"));
            Assert.Throws<RenderException>(() => templateLocalFileSystem.GetTemplate(context, "Bar"));
        }

        [Fact]
        public void GivenAValidTemplateDirectory_WhenReadTemplateWithContext_ExceptionShouldBeThrown()
        {
            var templateLocalFileSystem = new TemplateLocalFileSystem(TestConstants.ECRTemplateDirectory);
            var context = new TemplateContext(CultureInfo.InvariantCulture);
            context.SetValue("EICR", "EICR");
            Assert.Throws<NotImplementedException>(() => templateLocalFileSystem.ReadTemplateFile(context, "hello"));
        }
    }
}
