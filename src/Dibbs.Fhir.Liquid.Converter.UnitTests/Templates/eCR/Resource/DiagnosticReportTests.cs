using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests
{
    public class DiagnosticReportTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory, "Resource", "DiagnosticReport.liquid"
        );

        [Fact]
        public void DR_Status_GivenLabResultStatusObs_ReturnsStatusFromObs()
        {
            var attributes = new Dictionary<string, object>
            { { "ID", "123" }, { "diagnosticReport", Hash.FromAnonymousObject(new { component = new { observation = new { templateId = new { root = "2.16.840.1.113883.10.20.22.4.418" }, value = new { code = "F", displayName = "Final results; results stored and verified. Can only be changed with a corrected result.", codeSystem = "2.16.840.1.113883.18.51", codeSystemName = "HL7ResultStatus" } } } }) } };

            var actualContent = RenderLiquidTemplate(ECRPath, attributes);
            Assert.Contains(@"""status"":""final""", actualContent);
            Assert.Contains(@"""url"": ""http://terminology.hl7.org/CodeSystem/v2-0123""", actualContent);
        }

        [Fact]
        public void DR_Status_GivenNoLabResultStatusObs_ReturnsStatusFromStatusCode()
        {
            var attributes = new Dictionary<string, object>
            { { "ID", "123" }, { "diagnosticReport", Hash.FromAnonymousObject(new { statusCode = new { code = "active" } }) } };

            var actualContent = RenderLiquidTemplate(ECRPath, attributes);
            Assert.Contains(@"""status"":""preliminary""", actualContent);
            Assert.DoesNotContain(@"""url"": ""http://terminology.hl7.org/CodeSystem/v2-0123""", actualContent);
        }
    }
}
