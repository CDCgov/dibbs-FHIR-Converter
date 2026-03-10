using System.Collections.Generic;
using System.IO;
using Xunit;
using Hl7.Fhir.Model;
using Dibbs.Fhir.Liquid.Converter.DataParsers;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests
{
    public class QuestionnaireResponseTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory, "Resource", "QuestionnaireResponse.liquid"
        );

        [Fact]
        public void QuestionnaireResponse_CodeableConceptItem()
        {
            var xmlStr = @"
                <observation classCode=""OBS"" moodCode=""EVN"">
                  <templateId root=""2.16.840.1.113883.10.20.22.4.86"" />
                  <templateId extension=""2022-06-01""
                    root=""2.16.840.1.113883.10.20.22.4.86"" />
                  <id extension=""cer43239-3531400623-02433-Z1644335""
                    root=""1.2.840.114350.1.13.719.2.7.1.83687972"" />
                  <code code=""93157-6"" codeSystem=""2.16.840.1.113883.6.1""
                    codeSystemName=""LOINC""
                    displayName=""How often do you need to have someone help you when you read instructions, pamphlets, or other written material from your doctor or pharmacy [SILS]"">
                    <originalText>
                      <reference value=""#sdohassess1pair1ques"" />
                    </originalText>
                  </code>
                  <text>
                    <reference value=""#sdohassess1pair1"" />
                  </text>
                  <statusCode code=""completed"" />
                  <value
                    xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                    code=""LA6270-8"" codeSystem=""2.16.840.1.113883.6.1""
                    codeSystemName=""LOINC"" displayName=""Never""
                    xsi:type=""CD"">
                    <originalText>
                      <reference value=""#sdohassess1pair1ans"" />
                    </originalText>
                  </value>
                </observation>
            ";
            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "entry", parsed["observation"]},
            };

            var actualJson = GetJsonFromTemplate(ECRPath, attributes);
            Assert.Equal(ResourceType.QuestionnaireResponse.ToString(), actualJson.GetProperty("resourceType").GetString());
            Assert.NotNull(actualJson.GetProperty("id").GetString());

            var code = actualJson.GetProperty("code");
            Assert.Equal("How often do you need to have someone help you when you read instructions, pamphlets, or other written material from your doctor or pharmacy [OASIS]", code.GetProperty("coding")[0].GetProperty("display").GetString());
            Assert.Equal("http://loinc.org", code.GetProperty("coding")[0].GetProperty("system").GetString());
            Assert.Equal("93157-6", code.GetProperty("coding")[0].GetProperty("code").GetString());

            Assert.Equal(QuestionnaireResponse.QuestionnaireResponseStatus.Completed.ToString().ToLower(), actualJson.GetProperty("status").GetString());
            
            var item = actualJson.GetProperty("item");
            Assert.Equal("Never", item.GetProperty("value").GetProperty("coding")[0].GetProperty("display").GetString());
            Assert.Equal("http://loinc.org", item.GetProperty("value").GetProperty("coding")[0].GetProperty("system").GetString());
            Assert.Equal("LA6270-8", item.GetProperty("value").GetProperty("coding")[0].GetProperty("code").GetString());
        }
        
        [Fact]
        public void QuestionnaireResponse_TranslationItem()
        {
            var xmlStr = @"
                <observation classCode=""OBS"" moodCode=""EVN"">
                  <templateId root=""2.16.840.1.113883.10.20.22.4.86"" />
                  <templateId extension=""2022-06-01""
                    root=""2.16.840.1.113883.10.20.22.4.86"" />
                  <id extension=""cer90202-1581303289-74454-Z0088381""
                    root=""1.2.840.114350.1.13.719.2.7.1.83687972"" />
                  <code nullFlavor=""OTH"">
                    <originalText>
                      <reference value=""#sdohassess4pair1ques"" />
                    </originalText>
                    <translation code=""X-SDOH-FLO-1572879817""
                      codeSystem=""1.2.840.114350.1.72.1.8.1""
                      codeSystemName=""Epic.Sdoh""
                      displayName=""In the past 12 months, has lack of transportation kept you from medical appointments or from getting medications?"" />
                  </code>
                  <text>
                    <reference value=""#sdohassess4pair1"" />
                  </text>
                  <statusCode code=""completed"" />
                  <value
                    xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                    nullFlavor=""OTH"" xsi:type=""CD"">
                    <originalText>
                      <reference value=""#sdohassess4pair1ans"" />
                    </originalText>
                    <translation code=""X-SDOH-FLO-1572879817-1""
                      codeSystem=""1.2.840.114350.1.72.1.8.1""
                      codeSystemName=""Epic.Sdoh"" displayName=""Yes"" />
                  </value>
                </observation>
            ";
            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "entry", parsed["observation"]},
            };

            var actualJson = GetJsonFromTemplate(ECRPath, attributes);
            Assert.Equal(ResourceType.QuestionnaireResponse.ToString(), actualJson.GetProperty("resourceType").GetString());
            Assert.NotNull(actualJson.GetProperty("id").GetString());

            var code = actualJson.GetProperty("code");
            Assert.Equal("In the past 12 months, has lack of transportation kept you from medical appointments or from getting medications?", code.GetProperty("coding")[0].GetProperty("display").GetString());
            Assert.Equal("urn:oid:1.2.840.114350.1.72.1.8.1", code.GetProperty("coding")[0].GetProperty("system").GetString());
            Assert.Equal("X-SDOH-FLO-1572879817", code.GetProperty("coding")[0].GetProperty("code").GetString());

            Assert.Equal(QuestionnaireResponse.QuestionnaireResponseStatus.Completed.ToString().ToLower(), actualJson.GetProperty("status").GetString());
            
            var item = actualJson.GetProperty("item");
            Assert.Equal("Yes", item.GetProperty("value").GetProperty("coding")[0].GetProperty("display").GetString());
            Assert.Equal("urn:oid:1.2.840.114350.1.72.1.8.1", item.GetProperty("value").GetProperty("coding")[0].GetProperty("system").GetString());
            Assert.Equal("X-SDOH-FLO-1572879817-1", item.GetProperty("value").GetProperty("coding")[0].GetProperty("code").GetString());
        }
    }
}
