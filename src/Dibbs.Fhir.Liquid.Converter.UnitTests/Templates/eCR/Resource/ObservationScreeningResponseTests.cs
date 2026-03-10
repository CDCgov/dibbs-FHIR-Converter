using System.Collections.Generic;
using System.IO;
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

            var actualJson = GetJsonFromTemplate(ECRPath, attributes);
            Assert.Equal(ResourceType.Observation.ToString(), actualJson.GetProperty("resourceType").GetString());
            Assert.NotNull(actualJson.GetProperty("id").GetString());

            var code = actualJson.GetProperty("code");
            Assert.Equal("Hunger Vital Sign [HVS]", code.GetProperty("coding")[0].GetProperty("display").GetString());
            Assert.Equal("http://loinc.org", code.GetProperty("coding")[0].GetProperty("system").GetString());
            Assert.Equal("88121-9", code.GetProperty("coding")[0].GetProperty("code").GetString());
            Assert.Equal("Hunger Vital Sign", code.GetProperty("text").GetString());

            Assert.Equal(ObservationStatus.Final.ToString().ToLower(), actualJson.GetProperty("status").GetString());

            Assert.Equal("2025-02-05", actualJson.GetProperty("effectiveDateTime").GetString());
            
            var value = actualJson.GetProperty("value");
            Assert.Equal("High Risk", value.GetProperty("coding")[0].GetProperty("display").GetString());
            Assert.Equal("urn:oid:1.2.840.114350.1.72.1.8.1", value.GetProperty("coding")[0].GetProperty("system").GetString());
            Assert.Equal("X-SDOH-RISK-3", value.GetProperty("coding")[0].GetProperty("code").GetString());
            Assert.Equal("Food Insecurity Present", value.GetProperty("text").GetString());
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

            var actualJson = GetJsonFromTemplate(ECRPath, attributes);
            Assert.Equal(ResourceType.Observation.ToString(), actualJson.GetProperty("resourceType").GetString());
            Assert.NotNull(actualJson.GetProperty("id").GetString());

            var code = actualJson.GetProperty("code");
            Assert.Equal("Hunger Vital Sign [HVS]", code.GetProperty("coding")[0].GetProperty("display").GetString());
            Assert.Equal("http://loinc.org", code.GetProperty("coding")[0].GetProperty("system").GetString());
            Assert.Equal("88121-9", code.GetProperty("coding")[0].GetProperty("code").GetString());
            Assert.Equal("Hunger Vital Sign", code.GetProperty("text").GetString());

            Assert.Equal(ObservationStatus.Final.ToString().ToLower(), actualJson.GetProperty("status").GetString());

            Assert.Equal("2025-02-05", actualJson.GetProperty("effectiveDateTime").GetString());
            
            var value = actualJson.GetProperty("valueCodeableConcept");
            Assert.Equal("At risk", value.GetProperty("coding")[0].GetProperty("display").GetString());
            Assert.Equal("http://loinc.org", value.GetProperty("coding")[0].GetProperty("system").GetString());
            Assert.Equal("LA19952-3", value.GetProperty("coding")[0].GetProperty("code").GetString());
        }
    }
}
