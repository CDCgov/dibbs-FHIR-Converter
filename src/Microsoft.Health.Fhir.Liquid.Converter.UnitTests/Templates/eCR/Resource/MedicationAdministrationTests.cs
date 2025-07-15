using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotLiquid;
using Hl7.Fhir.Model;
using Xunit;

namespace Microsoft.Health.Fhir.Liquid.Converter.UnitTests
{
    public class MedicationAdministrationTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "Resource",
            "_MedicationAdministration.liquid"
        );

        [Fact]
        public void MedicationAdministration_AllFields()
        {
            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                {
                    "medicationAdministration",
                    Hash.FromAnonymousObject(
                        new
                        {
                            templateId = new { root = "2.16.840.1.113883.10.20.22.4.16", extension = "2014-06-09" },
                            id = new { root = "6c844c75-aa34-411c-b7bd-5e4a9f206e29", },
                            statusCode = new { code = "completed" },
                            effectiveTime = new object[] {
                                new {
                                    value = "202011071159-0700"
                                },
                                new {
                                    operator_ = "A",
                                    period = new {
                                        value = "12",
                                        unit = "h"
                                    }
                                }
                            },
                            routeCode = new {
                                code = "C38288",
                                codeSystem = "2.16.840.1.113883.3.26.1.1",
                                codeSystemName = "NCI Thesaurus",
                                displayName = "ORAL"
                            },
                            doseQuantity = new {
                                value = "1",
                                unit = "g"
                            },
                            entryRelationship = new object[] {
                                new {
                                    typeCode = "CAUS",
                                    observation = new {
                                        classCode = "OBS",
                                        moodCode = "EVN",
                                        templateId = new {
                                            root = "2.16.840.1.113883.10.20.15.2.3.37",
                                            extension = "2019-04-01"
                                        },
                                        id = new { root = "ab1791b0-5c71-11db-b0de-0800200c9a55" },
                                        code = new {
                                            code = "67540-5",
                                            codeSystem = "2.16.840.1.113883.6.1",
                                            codeSystemName = "LOINC",
                                            displayName = "Response to medication"
                                        },
                                        statusCode = new { code = "completed" },
                                        effectiveTime = new { low = new { value = "20201101" } },
                                        value = new {
                                            code = "268910001",
                                            codeSystem = "2.16.840.1.113883.6.96",
                                            codeSystemName = "SNOMED CT",
                                            displayName = "Patient's condition improved (finding)",
                                        }
                                    }
                                },
                                new {
                                    typeCode = "COMP",
                                    observation = new { 
                                        templateId = new { root = "2.16.840.1.113883.10.20.22.4.999" },
                                        value = new { code = "Red herring"}
                                    },
                                },
                            },
                        }
                    )
                },
            };
            var actualFhir = GetFhirObjectFromTemplate<MedicationAdministration>(ECRPath, attributes);
            Console.WriteLine("FOOBAR");

            Assert.Equal("MedicationAdministration", actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);
            // Assert.Equal(
            //     "http://hl7.org/fhir/us/core/StructureDefinition/us-core-procedure",
            //     actualFhir.Meta.Profile.First()
            // );
            // Assert.NotEmpty(actualFhir.Identifier);
            // Assert.Equal(EventStatus.NotDone, actualFhir.Status);
            // Assert.NotNull(actualFhir.Code);
            // Assert.NotNull(actualFhir.Performed);
            // Assert.NotEmpty(actualFhir.BodySite);
            // Assert.Equal("Why not", actualFhir.ReasonCode.First().Coding.First().Code);
            // Assert.Equal("Couldn't hurt", actualFhir.ReasonCode.Last().Coding.First().Code);
            // Assert.Equal(
            //     "METHOD", 
            //     actualFhir.GetExtensionValue<CodeableConcept>("http://hl7.org/fhir/StructureDefinition/procedure-method").Coding.First().Code
            // );
            // Assert.Equal(
            //     "CR", 
            //     actualFhir.GetExtensionValue<CodeableConcept>("priorityCode").Coding.First().Code
            // );
            // var specimen = actualFhir.GetExtensions("specimen");
            // Assert.Equal("Tissue", ((CodeableConcept)specimen.First().Value).Coding.First().Code);
            // Assert.Equal("Bile", ((CodeableConcept)specimen.Last().Value).Coding.First().Code);
        }
    }
}
