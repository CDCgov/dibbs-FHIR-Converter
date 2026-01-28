// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Globalization;
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

        private const string TestTemplate = @"{{ '\E' | escape_special_chars }}";

        private const string Expected = @"\\E";

        [Fact]
        public void FiltersRenderingTest()
        {
            var template = parser.Parse(TestTemplate);
            var templateOptions = new TemplateOptions();
            templateOptions.Filters.AddFilter("escape_special_chars", Filters.EscapeSpecialChars);
            var context = new TemplateContext(templateOptions);

            var actual = template.Render(context);
            Assert.Equal(Expected, actual);
        }
    }
}
