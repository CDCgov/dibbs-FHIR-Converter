using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using DotLiquid;
using Xunit;

namespace Microsoft.Health.Fhir.Liquid.Converter.UnitTests
{
    public class ObservationEmergencyOutbreakInformationTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "Resource",
            "_ObservationEmergencyOutbreakInformation.liquid"
        );

        [Fact]
        public void BasicReturnsNormal()
        {
            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                {
                    "observationEntry",
                    Hash.FromAnonymousObject(
                        new
                        {
                            id = new { root = "ab1791b0-5c71-11db-b0de-0800200c9a54", },
                            statusCode = new { code = "completed", },
                            code = new
                            {
                                originalText = new
                                {
                                    _ = "Distance of mail workers from mail sorter machines",
                                },
                            },
                            value = new
                            {
                                type = "PQ",
                                value = "2",
                                unit = "m",
                            },
                            effectiveTime = new { low = new { value = "20201101", }, },
                        }
                    )
                },
            };
            var expected = File.ReadAllText(
                Path.Join(
                    TestConstants.ExpectedDirectory,
                    "ObservationEmergencyOutbreakInformation.json"
                )
            );

            ConvertCheckLiquidTemplate(ECRPath, attributes, expected);
        }
    }
}
