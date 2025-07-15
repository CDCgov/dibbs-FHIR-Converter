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
                            },
                            participant = new {
                                typeCode = "IND",
                                participantRole = new {
                                    telecom = new {
                                        use= "WP",
                                        value = "555-1212"
                                    },
                                    addr = new {
                                        streetAddressLine = new {
                                         _ = "Peachtree St",
                                        },
                                        city = new {
                                         _ = "Atlanta",
                                        },
                                        state = new {
                                         _ = "Georgia",
                                        },
                                    },
                                    playingEntity = new {
                                        name = new {
                                        _ = "University Hospital"
                                        }
                                    }
                            }}
                        }
                    )
                },
            };
            var actualFhir = GetFhirObjectFromTemplate<Observation>(ECRPath, attributes);

            Assert.Equal(ResourceType.Observation.ToString(), actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);

            Assert.Equal(ObservationStatus.Final, actualFhir.Status);

            // Code
            Assert.NotNull(actualFhir.Code);
            Assert.Equal("History of Occupation", actualFhir.Code?.Text);
            Assert.Equal("2.16.840.1.113883.6.1", actualFhir.Code?.Coding?.First().System);

            // Effective date
            Assert.Equal("20201101", (actualFhir.Effective as FhirDateTime)?.Value);

            Assert.IsType<CodeableConcept>(actualFhir.Value);
            var occupation = (CodeableConcept)actualFhir.Value;

            Assert.Equal("3600", occupation.Coding.First().Code);
            Assert.Equal("U.S. Census Occupation Code (2010)", occupation.Coding.First().System);
            Assert.Equal("Nursing, psychiatric, and home health aides", occupation.Coding.First().Display);

            // Translation (ODH code)
            var translation = occupation.Coding.First().Extension
                ?.FirstOrDefault(e => e.Url == "http://hl7.org/fhir/StructureDefinition/translation")?.Value as Coding;

            Assert.NotNull(translation);
            Assert.Equal("31-1014.00.007136", translation.Code);
            Assert.Equal("2.16.840.1.114222.4.5.327", translation.System);
            Assert.Equal("Certified Nursing Assistant (CNA) [Nursing Assistants]", translation.Display);

            // Employer Reference
            Assert.NotNull(actualFhir.Subject?.Reference);
            Assert.Equal("Organization/4567", actualFhir.Subject.Reference);

            // Employer details (if included via extensions or components)
            var employerNameExt = actualFhir.GetExtensions("employerName").FirstOrDefault()?.Value as FhirString;
            Assert.Equal("University Hospital", employerNameExt?.Value);

            // Contact information
            var phoneExt = actualFhir.GetExtensions("employerPhone").FirstOrDefault()?.Value as ContactPoint;
            Assert.Equal("555-1212", phoneExt?.Value);
            Assert.Equal(ContactPoint.ContactPointUse.Work, phoneExt?.Use);

            //Address
            var addressExt = actualFhir.GetExtensions("employerAddress").FirstOrDefault()?.Value as Address;
            Assert.NotNull(addressExt);
            Assert.Contains("Peachtree St", addressExt.Line);
            Assert.Equal("Atlanta", addressExt.City);
            Assert.Equal("Georgia", addressExt.State);
        }
    }
}
