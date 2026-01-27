// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using Dibbs.Fhir.Liquid.Converter.DataParsers;
using Xunit;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests.FilterTests
{
    public class SectionFiltersTests
    {
        private static readonly Dictionary<string, object> TestData = LoadTestData();

        [Fact]
        public void GetFirstCcdaSectionsByTemplateIdTests()
        {
            const string templateIdContent = "2.16.840.1.113883.10.20.22.2.6.1";

            // Empty data
            Assert.Empty(Filters.GetFirstCcdaSectionsByTemplateId(new Hash(), templateIdContent));

            // Empty template id content
            Assert.Empty(Filters.GetFirstCcdaSectionsByTemplateId(Hash.FromDictionary(TestData), string.Empty));

            // Valid data and template id content
            var sections = Filters.GetFirstCcdaSectionsByTemplateId(Hash.FromDictionary(TestData), templateIdContent);
            Assert.Single(sections);
            Assert.Equal(5, ((Dictionary<string, object>)sections["2_16_840_1_113883_10_20_22_2_6_1"]).Count);

            // Null data or template id content
            Assert.Throws<NullReferenceException>(() => Filters.GetFirstCcdaSectionsByTemplateId(null, templateIdContent));
            Assert.Throws<NullReferenceException>(() => Filters.GetFirstCcdaSectionsByTemplateId(new Hash(), null));
        }

        private static Dictionary<string, object> LoadTestData()
        {
            var dataContent = File.ReadAllText(Path.Join(TestConstants.SampleDataDirectory, "Ccda", "170.314B2_Amb_CCD.ccda"));
            var parser = new CcdaDataParser();
            return parser.Parse(dataContent) as Dictionary<string, object>;
        }
    }
}
