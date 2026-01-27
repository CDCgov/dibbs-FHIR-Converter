using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hl7.Fhir.Model;
using Xunit;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests
{
    public class MedicationRequestTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "Resource",
            "MedicationRequest.liquid"
        );

        [Fact]
        public void MedicationRequest_AllFields()
        {
            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "patientReference", "Patient/4566" },
                { "intent", "plan"},
                {
                    "medicationRequest",
                    Hash.FromAnonymousObject(
                        new {
                            id = new { root = "ab1791b0-5c71-11db-b0de-0800200c9a54", },
                            moodCode = "RQO",
                            statusCode = new { code = "active" },
                            consumable = new {
                                manufacturedProduct = new {
                                    manufacturedMaterial = new { code = "med here" }
                                }
                            },
                            effectiveTime = new {
                                value = "20201101"
                            },
                            entryRelationship = new object[] {
                                new {
                                    typeCode = "RSON",
                                    observation = new { value = new { code = "Why not" } },
                                },
                                new {
                                    typeCode = "REFR",
                                    observation = new { 
                                        templateId = new { root = "2.16.840.1.113883.10.20.22.4.143" },
                                        value =  new { code = "A" } 
                                    },
                                },
                            },
                            author = new {
                                time = "20201030"
                            },
                            approachSiteCode = new { code = "Abdomen" },
                            routeCode = new {
                                code = "a code",
                                translation = new { code = "Synonym" }
                            },
                            doseQuantity = new { value = "3", unit = "mg" },
                            repeatNumber = new { value = "1" },
                        }
                    )
                },
            };
            var actualFhir = GetFhirObjectFromTemplate<MedicationRequest>(ECRPath, attributes);

            Assert.Equal("MedicationRequest", actualFhir.TypeName);
            Assert.Equal(MedicationRequest.MedicationRequestIntent.Plan, actualFhir.Intent);
            Assert.NotNull(actualFhir.Id);
            Assert.Equal(MedicationRequest.MedicationrequestStatus.Active, actualFhir.Status);
            Assert.Equal("Why not", actualFhir.ReasonCode.First().Coding.First().Code);
            Assert.Equal(RequestPriority.Asap , actualFhir.Priority);
            Assert.Equal(1, actualFhir.DispenseRequest.NumberOfRepeatsAllowed);
            var dosage = actualFhir.DosageInstruction.First();
            Assert.NotEmpty(dosage.Site);
            Assert.NotEmpty(dosage.DoseAndRate);
            Assert.NotEmpty(dosage.Route);
        }
    }
}
