// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;
using Dibbs.Fhir.Liquid.Converter.Utilities;
using Fluid;
using Xunit;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests.FilterTests
{
    public class FiltersRenderingTests
    {
        private readonly FluidParser parser;
        public FiltersRenderingTests()
        {
            parser = new FluidParser();
        }

        private const string TestTemplate = """
        {% assign test = "\"test\"" %}{{ test | escape_special_chars }}
        """;

        private const string Expected = "\\\"test\\\"";

        [Fact]
        public void FiltersRenderingTest()
        {
            var template = parser.Parse(TestTemplate);
            var context = new TemplateContext(TemplateUtility.TemplateOptions);

            var actual = template.Render(context);
            Assert.Equal(Expected, actual);
        }
    }
}
