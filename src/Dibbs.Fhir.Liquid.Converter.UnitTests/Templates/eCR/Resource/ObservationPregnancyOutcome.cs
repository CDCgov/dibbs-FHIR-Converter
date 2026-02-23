using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hl7.Fhir.Model;
using Xunit;
using Dibbs.Fhir.Liquid.Converter.DataParsers;
using System;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests
{
    public class ObservationPregnancyOutcomeTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "Resource",
            "ObservationPregnancyOutcome.liquid"
        );

        [Fact]
        public void ObservationPregnancyOutcome_AllFields()
        {
            // from 3.1 spec
            var xmlStr = @"
            <observation
                classCode=""OBS""
                moodCode=""EVN""
                xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                xsi:schemaLocation=""urn:hl7-org:v3 ../../../cda-core-2.0/schema/extensions/SDTC/infrastructure/cda/CDA_SDTC.xsd""
                xmlns=""urn:hl7-org:v3""
                xmlns:cda=""urn:hl7-org:v3""
                xmlns:sdtc=""urn:hl7-org:sdtc""
                xmlns:voc=""http://www.lantanagroup.com/voc""
                >
                <!-- [C-CDA PREG] Pregnancy Outcome -->
                <templateId root=""2.16.840.1.113883.10.20.22.4.284"" extension=""2018-04-01"" />
                <id root=""9af9cf32-b401-49b5-a817-97ba55d75dd2"" />
                <code code=""63893-2""
                    codeSystem=""2.16.840.1.113883.6.1""
                    displayName=""Outcome of Pregnancy""
                    codeSystemName=""LOINC"" />
                <statusCode code=""completed"" />
                <effectiveTime value=""20171004"" />
                <value xsi:type=""CD"" code=""21243004""
                    codeSystem=""2.16.840.1.113883.6.96""
                    displayName=""Term birth of newborn (finding)""
                    codeSystemName=""SNOMED CT"" />
                <entryRelationship typeCode=""REFR"">
                    <procedure classCode=""PROC"" moodCode=""EVN"">
                        <!-- [C-CDA R2.0] Procedure Activity Procedure -->
                        <templateId root=""2.16.840.1.113883.10.20.22.4.14"" extension=""2014-06-09"" />
                        <!-- [C-CDA PREG] Method of Delivery -->
                        <templateId root=""2.16.840.1.113883.10.20.22.4.299"" extension=""2018-04-01"" />
                    </procedure>
                </entryRelationship>
            </observation>
            ";
            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "birthOrder", "2" },
                { "observationEntry", parsed["observation"]},
            };

            var actualFhir = GetFhirObjectFromTemplate<Observation>(ECRPath, attributes);

            Assert.Equal(ResourceType.Observation.ToString(), actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);

            Assert.Equal(ObservationStatus.Final, actualFhir.Status);

            Assert.NotNull(actualFhir.Code);
            Assert.Equal("Outcome of pregnancy", actualFhir.Code?.Coding?.First().Display);
            Assert.Equal("http://loinc.org", actualFhir.Code?.Coding?.First().System);


            Assert.Equal("2017-10-04", (actualFhir.Effective as FhirDateTime)?.Value);

            Assert.IsType<CodeableConcept>(actualFhir.Value);
            var value = (CodeableConcept)actualFhir.Value;

            Assert.Equal("21243004", value.Coding.First().Code);
            Assert.Equal("http://snomed.info/sct", value.Coding.First().System);
            Assert.Equal("Term birth of newborn", value.Coding.First().Display);

            var components = actualFhir.Component;
            Assert.Equal(1, components.Count());
            Assert.Equal("2", components[0].Value.ToString());
        }
    }
}
