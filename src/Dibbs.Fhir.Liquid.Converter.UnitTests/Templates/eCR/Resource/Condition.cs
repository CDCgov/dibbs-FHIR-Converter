using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hl7.Fhir.Model;
using Xunit;
using Dibbs.Fhir.Liquid.Converter.DataParsers;
using System;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests
{
    public class ConditionTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "Resource",
            "Condition.liquid"
        );

        [Fact]
        public void Condition_AllFields()
        {
            // from Eve Everywoman
            var xmlStr = @"
                <observation classCode=""OBS"" moodCode=""EVN""
                    xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                    xsi:schemaLocation=""urn:hl7-org:v3 ../../../cda-core-2.0/schema/extensions/SDTC/infrastructure/cda/CDA_SDTC.xsd""
                    xmlns=""urn:hl7-org:v3""
                    xmlns:cda=""urn:hl7-org:v3""
                    xmlns:sdtc=""urn:hl7-org:sdtc""
                    xmlns:voc=""http://www.lantanagroup.com/voc""
                    negationInd=""false"">
                    <!-- [C-CDA R1.1] Problem Observation -->
                    <templateId root=""2.16.840.1.113883.10.20.22.4.4"" />
                    <!-- [C-CDA R2.1] Problem Observation (V3) -->
                    <templateId root=""2.16.840.1.113883.10.20.22.4.4""
                        extension=""2015-08-01"" />
                    <!-- [eICR R2 STU2] Initial Case Report Trigger Code
                    Problem Observation (V3) -->
                    <templateId root=""2.16.840.1.113883.10.20.15.2.3.3""
                        extension=""2021-01-01"" />
                    <id root=""db734647-fc99-424c-a864-7e3cda82e705"" />
                    <code code=""29308-4"" codeSystem=""2.16.840.1.113883.6.1""
                        codeSystemName=""LOINC"" displayName=""Diagnosis"">
                        <translation code=""282291009""
                            codeSystem=""2.16.840.1.113883.6.96""
                            codeSystemName=""SNOMED CT""
                            displayName=""Diagnosis"" />
                    </code>
                    <statusCode code=""completed"" />
                    <effectiveTime>
                        <low value=""20201107"" />
                    </effectiveTime>
                    <!-- Trigger code -->
                    <value xsi:type=""CD"" code=""27836007""
                        codeSystem=""2.16.840.1.113883.6.96""
                        codeSystemName=""SNOMED CT""
                        displayName=""Pertussis (disorder)""
                        sdtc:valueSet=""2.16.840.1.114222.4.11.7508""
                        sdtc:valueSetVersion=""2020-11-13"" />
                </observation>
            ";
            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "conditionEntry", parsed["observation"]},
            };

            var actualFhir = GetFhirObjectFromTemplate<Condition>(ECRPath, attributes);

            Assert.Equal(ResourceType.Condition.ToString(), actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);

            Assert.Equal("problem-item-list", actualFhir.Category[0].Coding[0].Code);
            Assert.Equal("27836007", actualFhir.Code.Coding[0].Code);
            Assert.Equal("Pertussis", actualFhir.Code.Coding[0].Display);
        }

        [Fact]
        public void ConditionNoKnownActiveProblems_AllFields()
        {
            var xmlStr = @"
                <observation classCode=""OBS"" moodCode=""EVN"" negationInd=""true"">
                    <templateId root=""2.16.840.1.113883.10.20.22.4.4""/>
                    <templateId extension=""2015-08-01"" root=""2.16.840.1.113883.10.20.22.4.4""/>
                    <templateId extension=""2022-06-01"" root=""2.16.840.1.113883.10.20.22.4.4""/>
                    <id nullFlavor=""NI""/>
                    <code code=""64572001"" codeSystem=""2.16.840.1.113883.6.96"" codeSystemName=""SNOMED CT""><translation code=""75323-6"" codeSystem=""2.16.840.1.113883.6.1"" codeSystemName=""LOINC""/></code>
                    <text>Foobar</text>
                    <statusCode code=""completed""/>
                    <effectiveTime>
                        <low nullFlavor=""UNK""/>
                    </effectiveTime>
                    <value xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" code=""55607006"" codeSystem=""2.16.840.1.113883.6.96"" codeSystemName=""SNOMED CT"" xsi:type=""CD"">
                        <originalText>No known active problems</originalText>
                    </value>
                    <author>
                        <templateId root=""2.16.840.1.113883.10.20.22.4.119""/>
                        <templateId extension=""2019-10-01"" root=""2.16.840.1.113883.10.20.22.5.6""/>
                        <time value=""20250731091502-0500""/>
                    </author>
                </observation>
            ";
            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "conditionEntry", parsed["observation"]},
            };

            var actualFhir = GetFhirObjectFromTemplate<Condition>(ECRPath, attributes);

            Assert.Equal(ResourceType.Condition.ToString(), actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);

            Assert.Equal("problem-item-list", actualFhir.Category[0].Coding[0].Code);
            Assert.Equal("160245001", actualFhir.Code.Coding[0].Code);
            Assert.Equal("No current problems or disability", actualFhir.Code.Coding[0].Display);
            Assert.Equal("No known active problems", actualFhir.Code.Text);
            Assert.Equal("Foobar", actualFhir.Note[0].Text);
        }
    }
}
