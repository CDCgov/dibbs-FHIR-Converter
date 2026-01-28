// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dibbs.Fhir.Liquid.Converter.DataParsers;
using Fluid;
using Fluid.Values;
using Xunit;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests.FilterTests
{
    public class SectionFiltersTests
    {
        private readonly TemplateContext context;
        public SectionFiltersTests()
        {
            context = new TemplateContext();
        }

        private static readonly Dictionary<string, object> TestData = LoadTestData();

        [Fact]
        public void GetFirstCcdaSectionsByTemplateIdTests()
        {
            const string templateIdContent = "2.16.840.1.113883.10.20.22.2.6.1";

            // Empty data
            Assert.Equal(0, (Filters.GetFirstCcdaSectionsByTemplateId(ObjectValue.Create(new object(), new TemplateOptions()), new FilterArguments(StringValue.Create(templateIdContent)), context).Result as DictionaryValue).Enumerate(context).Count());

            // Empty template id content
            Assert.Equal(0, (Filters.GetFirstCcdaSectionsByTemplateId(DictionaryValue.Create(TestData, new TemplateOptions()), new FilterArguments(StringValue.Create(string.Empty)), context).Result as DictionaryValue).Enumerate(context).Count());

            // Valid data and template id content
            var sections = Filters.GetFirstCcdaSectionsByTemplateId(DictionaryValue.Create(TestData, new TemplateOptions()), new FilterArguments(StringValue.Create(templateIdContent)), context).Result as DictionaryValue;
            Assert.Equal(1, sections.Enumerate(context).Count());
            Assert.Equal(5, (sections.GetValueAsync("2_16_840_1_113883_10_20_22_2_6_1", context).Result as DictionaryValue).Enumerate(context).Count());

            // Null data or template id content
            Assert.Throws<NullReferenceException>(() => Filters.GetFirstCcdaSectionsByTemplateId(NilValue.Instance, new FilterArguments(StringValue.Create(templateIdContent)), context));
            Assert.Throws<NullReferenceException>(() => Filters.GetFirstCcdaSectionsByTemplateId(ObjectValue.Create(new object(), new TemplateOptions()), FilterArguments.Empty, context));
        }

        private static Dictionary<string, object> LoadTestData()
        {
            var dataContent = File.ReadAllText(Path.Join(TestConstants.SampleDataDirectory, "Ccda", "170.314B2_Amb_CCD.ccda"));
            var parser = new CcdaDataParser();
            return parser.Parse(dataContent) as Dictionary<string, object>;
        }
    }
}
