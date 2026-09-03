using System.Collections.Generic;
using System.IO;
using Hl7.Fhir.Model;
using Xunit;
using Dibbs.Fhir.Liquid.Converter.DataParsers;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests
{
    public class ObservationSexualOrientationTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory, "Resource", "ObservationSexualOrientation.liquid"
        );

        [Fact]
        public void ObservationSexualOrientation_UsesCdaIdentifier()
        {
            var xmlStr = @"
                <observation classCode=""OBS"" moodCode=""EVN"">
                    <id root=""1.2.3.4"" extension=""sexual-orientation-id"" />
                    <id root=""22222222-2222-2222-2222-222222222222"" />
                    <code code=""76690-7"" codeSystem=""2.16.840.1.113883.6.1""
                        displayName=""Sexual orientation"" />
                    <statusCode code=""completed"" />
                    <effectiveTime value=""20200101"" />
                    <value xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                        xsi:type=""CD"" code=""20430005"" codeSystem=""2.16.840.1.113883.6.96""
                        displayName=""Heterosexual"" />
                </observation>
            ";
            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "observationEntry", parsed["observation"] },
            };

            var actualFhir = GetFhirObjectFromTemplate<Observation>(ECRPath, attributes);

            Assert.Collection(
                actualFhir.Identifier,
                identifier =>
                {
                    Assert.Equal("urn:oid:1.2.3.4", identifier.System);
                    Assert.Equal("sexual-orientation-id", identifier.Value);
                },
                identifier =>
                {
                    Assert.Equal("urn:ietf:rfc:3986", identifier.System);
                    Assert.Equal("urn:uuid:22222222-2222-2222-2222-222222222222", identifier.Value);
                }
            );
        }
    }
}
