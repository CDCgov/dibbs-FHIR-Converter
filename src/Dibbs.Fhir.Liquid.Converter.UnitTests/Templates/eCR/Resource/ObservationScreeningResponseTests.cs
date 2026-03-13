using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Hl7.Fhir.Model;
using Dibbs.Fhir.Liquid.Converter.DataParsers;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests
{
    public class ObservationScreeningResponseTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory, "Resource", "ObservationScreeningResponse.liquid"
        );

        [Fact]
        public void ObservationScreeningResponse_Basic_AllFields()
        {
            var xmlStr = @"
                <observation classCode=""OBS"" moodCode=""EVN"">
                  <templateId root=""2.16.840.1.113883.10.20.22.4.69"" />
                  <templateId extension=""2022-06-01""
                    root=""2.16.840.1.113883.10.20.22.4.69"" />
                  <id extension=""7988992429-85750-Z6733056""
                    root=""1.2.840.114350.1.13.719.2.7.1.83687972"" />
                  <code code=""88121-9"" codeSystem=""2.16.840.1.113883.6.1""
                    codeSystemName=""LOINC"" displayName=""Hunger Vital Sign [HVS]"">
                    <originalText>Hunger Vital Sign</originalText>
                  </code>
                  <statusCode code=""completed"" />
                  <effectiveTime value=""20250205"" />
                  <value xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                    nullFlavor=""UNK"" xsi:type=""CD"" />
                  <interpretationCode nullFlavor=""OTH"">
                    <originalText>Food Insecurity Present</originalText>
                    <translation code=""X-SDOH-RISK-3""
                      codeSystem=""1.2.840.114350.1.72.1.8.1""
                      codeSystemName=""Epic.Sdoh"" displayName=""High Risk"" />
                  </interpretationCode>
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

            Assert.NotNull(actualFhir.Code);
            Assert.Equal("Hunger Vital Sign [HVS]", actualFhir.Code?.Coding?.First().Display);
            Assert.Equal("http://loinc.org", actualFhir.Code?.Coding?.First().System);
            Assert.Equal("88121-9", actualFhir.Code?.Coding?.First().Code);
            Assert.Equal("Hunger Vital Sign", actualFhir.Code?.Text);

            Assert.Equal(ObservationStatus.Final, actualFhir.Status);

            Assert.Equal("2025-02-05", (actualFhir.Effective as FhirDateTime)?.Value);

            Assert.IsType<CodeableConcept>(actualFhir.Value);
            var value = (CodeableConcept)actualFhir.Value;
            
            Assert.Equal("High Risk", value.Coding.First().Display);
            Assert.Equal("urn:oid:1.2.840.114350.1.72.1.8.1", value.Coding.First().System);
            Assert.Equal("X-SDOH-RISK-3", value.Coding.First().Code);
            Assert.Equal("Food Insecurity Present", value.Text);
        }

        [Fact]
        public void ObservationScreeningResponse_NoInterpretationCode()
        {
            var xmlStr = @"
                <observation classCode=""OBS"" moodCode=""EVN"">
                  <templateId root=""2.16.840.1.113883.10.20.22.4.69"" />
                  <templateId extension=""2022-06-01""
                    root=""2.16.840.1.113883.10.20.22.4.69"" />
                  <id extension=""7988992429-85750-Z6733056""
                    root=""1.2.840.114350.1.13.719.2.7.1.83687972"" />
                  <code code=""88121-9"" codeSystem=""2.16.840.1.113883.6.1""
                    codeSystemName=""LOINC"" displayName=""Hunger Vital Sign [HVS]"">
                    <originalText>Hunger Vital Sign</originalText>
                  </code>
                  <statusCode code=""completed"" />
                  <effectiveTime value=""20250205"" />
                  <value code=""LA19952-3"" codeSystem=""http://loinc.org"" codeSystemName=""LOINC""
                    displayName=""At risk""/>
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

            Assert.NotNull(actualFhir.Code);
            Assert.Equal("Hunger Vital Sign [HVS]", actualFhir.Code?.Coding?.First().Display);
            Assert.Equal("http://loinc.org", actualFhir.Code?.Coding?.First().System);
            Assert.Equal("88121-9", actualFhir.Code?.Coding?.First().Code);
            Assert.Equal("Hunger Vital Sign", actualFhir.Code?.Text);

            Assert.Equal(ObservationStatus.Final, actualFhir.Status);

            Assert.Equal("2025-02-05", (actualFhir.Effective as FhirDateTime)?.Value);

            Assert.IsType<CodeableConcept>(actualFhir.Value);
            var value = (CodeableConcept)actualFhir.Value;
            
            Assert.Equal("At risk", value.Coding.First().Display);
            Assert.Equal("http://loinc.org", value.Coding.First().System);
            Assert.Equal("LA19952-3", value.Coding.First().Code);
            Assert.Null(value.Text);
        }
    }
}
