using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hl7.Fhir.Model;
using Dibbs.Fhir.Liquid.Converter.DataParsers;
using Xunit;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests
{
    public class ObservationPostpartumStatusTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "Resource",
            "ObservationPostpartumStatus.liquid"
        );

        [Fact]
        public void ObservationPostpartumStatus_AllFields()
        {
            // from 3.1 spec
            var xmlStr =
                @"
                <observation
                    classCode=""OBS""
                    moodCode=""EVN""
                    xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                    xsi:schemaLocation=""urn:hl7-org:v3 ../../../cda-core-2.0/schema/extensions/SDTC/infrastructure/cda/CDA_SDTC.xsd""
                    xmlns=""urn:hl7-org:v3""
                    xmlns:cda=""urn:hl7-org:v3""
                    xmlns:sdtc=""urn:hl7-org:sdtc""
                    xmlns:voc=""http://www.lantanagroup.com/voc"">
                    <templateId root=""2.16.840.1.113883.10.20.22.4.285"" extension=""2020-04-01""/>
                    <id root=""9701b264-0f70-47f9-bfbf-aa4f9686cd3a""/>
                    <code code=""249197004"" codeSystem=""2.16.840.1.113883.6.96"" codeSystemName=""SNOMED CT"" displayName=""Maternal condition during puerperium (observable entity)""/>
                    <statusCode code=""completed""/>
                    <effectiveTime value=""202001051015""/>
                    <value xsi:type=""CD"" code=""42814007"" codeSystem=""2.16.840.1.113883.6.96"" codeSystemName=""SNOMED CT""
                    displayName=""Mid postpartum state (finding)""/>
                 </observation>
            ";
            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "observationEntry", parsed["observation"] },
            };

            var actualFhir = GetFhirObjectFromTemplate<Observation>(ECRPath, attributes);

            Assert.Equal(ResourceType.Observation.ToString(), actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);

            Assert.Equal(ObservationStatus.Final, actualFhir.Status);

            Assert.NotNull(actualFhir.Code);
            Assert.Equal(
                "Maternal condition during puerperium (observable entity)",
                actualFhir.Code?.Coding?.First().Display
            );
            Assert.Equal("http://snomed.info/sct", actualFhir.Code?.Coding?.First().System);

            Assert.Equal("2020-01-05T10:15:00", (actualFhir.Effective as FhirDateTime)?.Value);

            Assert.Equal("42814007", (actualFhir.Value as CodeableConcept).Coding.First().Code);
        }
    }
}
