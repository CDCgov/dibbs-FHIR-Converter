using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
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
        public void QuestionnaireResponse_CodeAnswer()
        {
            var xmlStr = @"
                <observation classCode=""OBS"" moodCode=""EVN"">
                  <templateId root=""2.16.840.1.113883.10.20.22.4.86"" />
                  <templateId extension=""2022-06-01""
                    root=""2.16.840.1.113883.10.20.22.4.86"" />
                  <id extension=""cer99351-7166272929-18651-Z4134990""
                    root=""1.2.840.114350.1.13.719.2.7.1.83687972"" />
                  <code code=""98976-4"" codeSystem=""2.16.840.1.113883.6.1""
                    codeSystemName=""LOINC""
                    displayName=""In the past 12 months, was there a time when you were not able to pay the mortgage or rent on time?"">
                    <originalText>In the past 12 months, was there a time when you were not able to pay the mortgage or rent on time?</originalText>
                  </code>
                  <text>
                    <reference value=""#sdohassess5pair1"" />
                  </text>
                  <statusCode code=""completed"" />
                  <value
                    xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                    code=""LA33-6"" codeSystem=""2.16.840.1.113883.6.1""
                    codeSystemName=""LOINC"" displayName=""Yes""
                    xsi:type=""CD"">
                  </value>
                </observation>
            ";
            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "entry", parsed["observation"]},
            };
            
            var actualFhir = GetFhirObjectFromTemplate<QuestionnaireResponse>(ECRPath, attributes);
            
            Assert.Equal(ResourceType.QuestionnaireResponse.ToString(), actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);

            Assert.Equal(QuestionnaireResponse.QuestionnaireResponseStatus.Completed, actualFhir.Status);

            Assert.Equal("In the past 12 months, was there a time when you were not able to pay the mortgage or rent on time?", actualFhir.Item[0].Text);
            
            Assert.IsType<Coding>(actualFhir.Item[0].Answer[0].Value);
            var value = (Coding)actualFhir.Item[0].Answer[0].Value;
            Assert.Equal("Yes", value.Display);
            Assert.Equal("LA33-6", value.Code);
            Assert.Equal("http://loinc.org", value.System);
        }
        
        [Fact]
        public void QuestionnaireResponse_TranslationAnswer()
        {
            var xmlStr = @"
                <observation classCode=""OBS"" moodCode=""EVN"">
                  <templateId root=""2.16.840.1.113883.10.20.22.4.86"" />
                  <templateId extension=""2022-06-01""
                    root=""2.16.840.1.113883.10.20.22.4.86"" />
                  <id extension=""cer90203-1581303289-74454-Z0088381""
                    root=""1.2.840.114350.1.13.719.2.7.1.83687972"" />
                  <code nullFlavor=""OTH"">
                    <originalText>In the past 12 months, has lack of transportation kept you from meetings, work, or from getting things needed for daily living?</originalText>
                    <translation code=""X-SDOH-FLO-1572879818""
                      codeSystem=""1.2.840.114350.1.72.1.8.1""
                      codeSystemName=""Epic.Sdoh""
                      displayName=""In the past 12 months, has lack of transportation kept you from meetings, work, or from getting things needed for daily living?"" />
                  </code>
                  <text>
                    <reference value=""#sdohassess4pair2"" />
                  </text>
                  <statusCode code=""completed"" />
                  <value
                    xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                    nullFlavor=""OTH"" xsi:type=""CD"">
                    <translation code=""X-SDOH-FLO-1572879818-1""
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
            
            var actualFhir = GetFhirObjectFromTemplate<QuestionnaireResponse>(ECRPath, attributes);
            
            Assert.Equal(ResourceType.QuestionnaireResponse.ToString(), actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);

            Assert.Equal(QuestionnaireResponse.QuestionnaireResponseStatus.Completed, actualFhir.Status);

            Assert.Equal("In the past 12 months, has lack of transportation kept you from meetings, work, or from getting things needed for daily living?", actualFhir.Item[0].Text);
            
            Assert.IsType<Coding>(actualFhir.Item[0].Answer[0].Value);
            var value = (Coding)actualFhir.Item[0].Answer[0].Value;
            Assert.Equal("Yes", value.Display);
            Assert.Equal("X-SDOH-FLO-1572879818-1", value.Code);
            Assert.Equal("urn:oid:1.2.840.114350.1.72.1.8.1", value.System);
        }

        [Fact]
        public void QuestionnaireResponse_OriginalTextStringAnswer()
        {
            var xmlStr = @"
                <observation classCode=""OBS"" moodCode=""EVN"">
                  <templateId root=""2.16.840.1.113883.10.20.22.4.86"" />
                  <templateId extension=""2022-06-01""
                    root=""2.16.840.1.113883.10.20.22.4.86"" />
                  <id extension=""cer66881-1040308556-97391-Z7090461""
                    root=""1.2.840.114350.1.13.719.2.7.1.83687972"" />
                  <code code=""72166-2"" codeSystem=""2.16.840.1.113883.6.1""
                    codeSystemName=""LOINC""
                    displayName=""Tobacco smoking status"">
                    <originalText>Smoking Tobacco Use</originalText>
                  </code>
                  <statusCode code=""completed"" />
                  <value
                    xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                    nullFlavor=""OTH"" xsi:type=""CD"">
                    <originalText>Never Assessed</originalText>
                  </value>
                </observation>
            ";
            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "entry", parsed["observation"]},
            };
            
            var actualFhir = GetFhirObjectFromTemplate<QuestionnaireResponse>(ECRPath, attributes);
            
            Assert.Equal(ResourceType.QuestionnaireResponse.ToString(), actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);

            Assert.Equal(QuestionnaireResponse.QuestionnaireResponseStatus.Completed, actualFhir.Status);

            Assert.Equal("Smoking Tobacco Use", actualFhir.Item[0].Text);
            Assert.Equal("Never Assessed", actualFhir.Item[0].Answer[0].Value.ToString());
        }
        
        [Fact]
        public void QuestionnaireResponse_AllNull()
        {
            var xmlStr = @"
                <observation classCode=""OBS"" moodCode=""EVN"">
                    <templateId root=""2.16.840.1.113883.10.20.22.4.86"" />
                    <templateId root=""2.16.840.1.113883.10.20.22.4.86""
                        extension=""2022-06-01"" />
                    <id root=""1.2.840.114350.1.13.4304.2.7.1.83687972""
                        extension=""XXh66838-3936872386-73162-X0893"" />
                    <code nullFlavor=""UNK"">
                        <originalText>REMOVED</originalText>
                    </code>
                    <statusCode code=""completed"" />
                    <value
                        xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                        xsi:type=""CD"" nullFlavor=""OTH"">
                        <originalText>Not on file</originalText>
                    </value>
                </observation>
            ";
            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "entry", parsed["observation"]},
            };
            
            var actualFhir = GetFhirObjectFromTemplate<QuestionnaireResponse>(ECRPath, attributes);
            
            Assert.Equal(ResourceType.QuestionnaireResponse.ToString(), actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);

            Assert.Equal(QuestionnaireResponse.QuestionnaireResponseStatus.Completed, actualFhir.Status);

            Assert.Equal("REMOVED", actualFhir.Item[0].Text);
            Assert.Equal("Not on file", actualFhir.Item[0].Answer[0].Value.ToString());
        }
        
        [Fact]
        public void QuestionnaireResponse_QuantityAnswer()
        {
            var xmlStr = @"
                <observation classCode=""OBS"" moodCode=""EVN"">
                    <templateId root=""2.16.840.1.113883.10.20.22.4.86""/>
                    <templateId root=""2.16.840.1.113883.10.20.22.4.86"" extension=""2022-06-01""/>
                    <id root=""1.2.840.114350.1.13.229.2.7.1.83687972"" extension=""cer97036-5767116322-97007-Z260359""/>
                    <code code=""89555-7"" codeSystem=""2.16.840.1.113883.6.1""
                        codeSystemName=""LOINC"" displayName=""For an average week in the last 30 days, how many days per week did you engage in moderate to strenuous exercise (like walking fast, running, or other activities that cause a light or heavy sweat)?"">
                        <originalText>For an average week in the last 30 days, how many days per week did you engage in moderate to strenuous exercise (like walking fast, running, or other activities that cause a light or heavy sweat)?</originalText>
                    </code>
                    <text>
                        <reference value=""#sdohassess3pair1""/>
                    </text>
                    <statusCode code=""completed""/>
                    <value xsi:type=""PQ"" value=""0"" unit=""d/wk""
                        xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""/>
                </observation>
            ";
            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "entry", parsed["observation"]},
            };
            
            var actualFhir = GetFhirObjectFromTemplate<QuestionnaireResponse>(ECRPath, attributes);
            
            Assert.Equal(ResourceType.QuestionnaireResponse.ToString(), actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);

            Assert.Equal(QuestionnaireResponse.QuestionnaireResponseStatus.Completed, actualFhir.Status);

            Assert.Equal("For an average week in the last 30 days, how many days per week did you engage in moderate to strenuous exercise (like walking fast, running, or other activities that cause a light or heavy sweat)?", actualFhir.Item[0].Text);
            
            Assert.IsType<Quantity>(actualFhir.Item[0].Answer[0].Value);
            var value = (Quantity)actualFhir.Item[0].Answer[0].Value;
            Assert.Equal(0, value.Value);
            Assert.Equal("d/wk", value.Unit);
        }
    }
}
