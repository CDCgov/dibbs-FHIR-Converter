using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotLiquid;
using Hl7.Fhir.Model;
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
        public void OutbreakInfo_AllFields()
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
            var actualFhir = GetFhirObjectFromTemplate<Observation>(ECRPath, attributes);

            Assert.Equal("Observation", actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);
            Assert.Equal(
                "http://hl7.org/fhir/us/ecr/StructureDefinition/us-ph-emergency-outbreak-information",
                actualFhir.Meta.Profile.First()
            );
            Assert.NotEmpty(actualFhir.Identifier);
            Assert.Equal("Final", actualFhir.Status.ToString());
            Assert.NotNull(actualFhir.Code);
            Assert.NotNull(actualFhir.Value);
            Assert.NotNull(actualFhir.Effective);
        }

        [Fact]
        public void OutbreakInfo_MinimumFields()
        {
            var attributes = new Dictionary<string, object>
            {
                {
                    "observationEntry",
                    Hash.FromAnonymousObject(
                        new
                        {
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
                        }
                    )
                },
            };
            var actualFhir = GetFhirObjectFromTemplate<Observation>(ECRPath, attributes);

            Assert.Equal("Observation", actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);
            Assert.Equal(
                "http://hl7.org/fhir/us/ecr/StructureDefinition/us-ph-emergency-outbreak-information",
                actualFhir.Meta.Profile.First()
            );
            Assert.Empty(actualFhir.Identifier);
            Assert.Equal("Final", actualFhir.Status.ToString());
            Assert.NotNull(actualFhir.Code);
            Assert.NotNull(actualFhir.Value);
            Assert.Null(actualFhir.Effective);
        }

        [Fact]
        public void OutbreakInfo_Snapshot()
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

            CompareJSONOutput(ECRPath, attributes, "ObservationEmergencyOutbreakInformation.json");
        }
    }
}
