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
        public void QuestionnaireResponse_StringAnswer()
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
    }
}
