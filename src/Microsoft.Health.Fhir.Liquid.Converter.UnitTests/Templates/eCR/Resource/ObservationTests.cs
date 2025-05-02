using System.Collections.Generic;
using System.IO;
using DotLiquid;
using Xunit;

namespace Microsoft.Health.Fhir.Liquid.Converter.UnitTests
{
    public class ObservationTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory, "Resource", "_Observation.liquid"
        );

        [Fact]
        public void Obs_Status_GivenLabObsResultStatus_ReturnsStatusFromObs()
        {
            var attributes = new Dictionary<string, object>
            { { "ID", "123" }, { "observationEntry", Hash.FromAnonymousObject(new { entryRelationship = new { observation = new { templateId = new { root = "2.16.840.1.113883.10.20.22.4.419" }, value = new { code = "P", displayName = "Preliminary results", codeSystem = "2.16.840.1.113883.18.34", codeSystemName = "HL7ObservationResultStatusCodesInterpretation" } } } }) } };

            var actualContent = RenderLiquidTemplate(ECRPath, attributes);
            Assert.Contains(@"""status"":""preliminary""", actualContent);
            Assert.Contains(@"""url"": ""http://terminology.hl7.org/ValueSet/v2-0085""", actualContent);
        }

        [Fact]
        public void Obs_Status_GivenNoLabObsResultStatus_ReturnsStatusFromStatusCode()
        {
            var attributes = new Dictionary<string, object>
            { { "ID", "123" }, { "observationEntry", Hash.FromAnonymousObject(new { statusCode = new { code = "active" } }) } };

            var actualContent = RenderLiquidTemplate(ECRPath, attributes);
            Assert.Contains(@"""status"":""preliminary""", actualContent);
            Assert.DoesNotContain(@"""url"": ""http://terminology.hl7.org/ValueSet/v2-0085""", actualContent);
        }
    }
}
