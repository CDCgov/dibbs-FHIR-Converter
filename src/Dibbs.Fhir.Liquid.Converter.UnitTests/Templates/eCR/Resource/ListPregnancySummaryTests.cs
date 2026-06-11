using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hl7.Fhir.ElementModel.Types;
using Hl7.Fhir.Model;
using Dibbs.Fhir.Liquid.Converter.DataParsers;
using Namotion.Reflection;
using Xunit;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests
{
    public class ListPregnancySummaryTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "Resource",
            "ListPregnancySummary.liquid"
        );

        [Fact]
        public void PregnancySummary_AllFields()
        {
            var xmlString =
                @"
                <organizer classCode=""CLUSTER"" moodCode=""EVN"">
                    <!-- [C-CDA PREG] Pregnancy Summary Organizer -->
                    <templateId root=""2.16.840.1.113883.10.20.22.4.292"" extension=""2018-04-01"" />
                    <id root=""0a648e8a-c61c-46dd-bd0c-3081db0a9f66"" />
                    <code code=""10162-6""
                        displayName=""History of Pregnancies Narrative""
                        codeSystem=""2.16.840.1.113883.6.1""
                        codeSystemName=""LOINC"" />
                    <statusCode code=""active"" />
                    <effectiveTime value=""201801051015""/>
                    <component>
                        <observation classCode=""OBS"" moodCode=""EVN"">
                            <!-- [C-CDA PREG] Gravidity (Total Pregnancies) -->
                            <templateId root=""2.16.840.1.113883.10.20.22.4.282"" extension=""2018-04-01"" />
                            <id root=""18701808-d4cb-4a6c-b10b-4eaeb66f1158"" />
                            <code code=""11996-6""
                                displayName=""[#] Pregnancies""
                                codeSystem=""2.16.840.1.113883.6.1""
                                codeSystemName=""LOINC"" />
                            <statusCode code=""completed"" />
                            <effectiveTime value=""201801051015"" />
                            <value xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""INT"" value=""6"" />
                        </observation>
                    </component>
                </organizer>";

            var parser = new CcdaDataParser();
            var parsedXml = parser.Parse(xmlString) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "entry", parsedXml["organizer"] },
            };

            var actualFhir = GetFhirObjectFromTemplate<List>(ECRPath, attributes);

            Assert.Equal("List", actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);
            Assert.NotEmpty(actualFhir.Identifier);
            Assert.Equal("Current", actualFhir.Status.ToString());

            Assert.Equal("http://loinc.org", actualFhir.Code.Coding.First().System);
            Assert.Equal("10162-6", actualFhir.Code.Coding.First().Code);
            Assert.Equal("History of pregnancies Narrative", actualFhir.Code.Coding.First().Display);

            Assert.Equal("2018-01-05T10:15:00", actualFhir.Date);
        }
    }
}
