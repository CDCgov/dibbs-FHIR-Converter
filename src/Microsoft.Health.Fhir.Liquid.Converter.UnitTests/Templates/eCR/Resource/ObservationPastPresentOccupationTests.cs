using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotLiquid;
using Hl7.Fhir.Model;
using Xunit;

namespace Microsoft.Health.Fhir.Liquid.Converter.UnitTests
{
    public class ObservationPastPresentOccupationTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "Resource",
            "_ObservationPastPresentOccupation.liquid"
        );

        [Fact]
        public void ObservationPastPresentOccupation_AllFields()
        {
            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "employerRef", "Organization/4567" },
                {
                    "observationEntry",
                    Hash.FromAnonymousObject(
                        new
                        {
                            id = new { root = "ab1791b0-5c71-11db-b0de-0800200c9a54", },
                            statusCode = new { code = "final", },
                            code = new
                            {
                                displayName = "History of Occupation",
                                codeSystem = "2.16.840.1.113883.6.1",
                                codeSystemName="LOINC"
                            },
                            effectiveTime = new {
                                value = "20201101"
                            },
                            text = "Nurse",
                            value = new {
                                code="3600",
                                codeSystem="2.16.840.1.113883.6.240",
                                codeSystemName="U.S. Census Occupation Code (2010)",
                                displayName="Nursing, psychiatric, and home health aides",
                                translation = new {
                                    code="31-1014.00.007136",
                                    codeSystem="2.16.840.1.114222.4.5.327",
                                    codeSystemName="Occupational Data for Health (ODH)",
                                    displayName="Certified Nursing Assistant (CNA) [Nursing Assistants]"
                                }
                            }
                        }
                    )
                },
            };

            var actualFhir = GetFhirObjectFromTemplate<Observation>(ECRPath, attributes);

            Assert.Equal(ResourceType.Observation.ToString(), actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);

            Assert.Equal(ObservationStatus.Final, actualFhir.Status);

            Assert.NotNull(actualFhir.Code);
            Assert.Equal("History of Occupation", actualFhir.Code?.Coding?.First().Display);
            Assert.Equal("http://loinc.org", actualFhir.Code?.Coding?.First().System);


            Assert.Equal("2020-11-01", (actualFhir.Effective as FhirDateTime)?.Value);

            Assert.IsType<CodeableConcept>(actualFhir.Value);
            var occupation = (CodeableConcept)actualFhir.Value;

            Assert.Equal("3600", occupation.Coding.First().Code);
            Assert.Equal("urn:oid:2.16.840.1.113883.6.240", occupation.Coding.First().System);
            Assert.Equal("Nursing, psychiatric, and home health aides", occupation.Coding.First().Display);
        }
    }
}
