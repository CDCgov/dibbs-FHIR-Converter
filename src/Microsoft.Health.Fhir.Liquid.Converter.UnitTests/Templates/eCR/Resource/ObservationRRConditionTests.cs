using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotLiquid;
using Hl7.Fhir.Model;
using Xunit;
using Microsoft.Health.Fhir.Liquid.Converter.Parsers;
using System;

namespace Microsoft.Health.Fhir.Liquid.Converter.UnitTests
{
    public class ObservationRRConditionTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "Resource",
            "_ObservationRRCondition.liquid"
        );

        [Fact]
        public void ObservationRRCondition_AllFields()
        {
            // from 3.1 eCR
            var xmlStr = @"
            <observation classCode=""OBS"" moodCode=""EVN""
                xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                xsi:schemaLocation=""urn:hl7-org:v3 ../../../cda-core-2.0/schema/extensions/SDTC/infrastructure/cda/CDA_SDTC.xsd""
                xmlns=""urn:hl7-org:v3""
                xmlns:cda=""urn:hl7-org:v3""
                xmlns:sdtc=""urn:hl7-org:sdtc""
                xmlns:voc=""http://www.lantanagroup.com/voc"">
                <templateId extension=""2017-04-01""
                    root=""2.16.840.1.113883.10.20.15.2.3.12"" />
                <id root=""a054d401-7b23-4b15-bc28-c889c156ba6a"" />
                <code code=""64572001"" codeSystem=""2.16.840.1.113883.6.96""
                    codeSystemName=""SNOMED"" displayName=""Condition"">
                    <translation code=""75323-6""
                        codeSystem=""2.16.840.1.113883.6.1""
                        codeSystemName=""LOINC"" displayName=""Condition"" />
                </code>
                <value code=""3928002"" codeSystem=""2.16.840.1.113883.6.96""
                    codeSystemName=""SNOMED CT""
                    displayName=""Zika virus disease (disorder)"" xsi:type=""CD"" />
            </observation>
            ";

            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;
            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "observationEntry", parsed["observation"]},
            };
            var actualFhir = GetFhirObjectFromTemplate<Observation>(ECRPath, attributes);

            Assert.Equal(ResourceType.Observation.ToString(), actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);
            Assert.Equal(ObservationStatus.Final, actualFhir.Status);

            Assert.NotNull(actualFhir.Code);
            Assert.Equal("http://snomed.info/sct", actualFhir.Code?.Coding?.First().System);
            Assert.Equal("Condition", actualFhir.Code?.Coding?.First().Display);
            Assert.Equal("64572001", actualFhir.Code?.Coding?.First().Code);

            Assert.IsType<CodeableConcept>(actualFhir.Value);
            var value = (CodeableConcept)actualFhir.Value;
            Assert.Equal("3928002", value.Coding.First().Code);
            Assert.Equal("http://snomed.info/sct", value.Coding.First().System);
            Assert.Equal("Zika virus disease", value.Coding.First().Display);
        }
    }
}