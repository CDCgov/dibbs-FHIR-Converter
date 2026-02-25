using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Hl7.Fhir.Model;
using Xunit;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests
{
    public class MedicationAdministrationTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "Resource",
            "MedicationAdministration.liquid"
        );

        [Fact]
        public void MedicationAdministration_AllFields()
        {
            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                {
                    "medicationAdministration",
                    new
                    {
                        templateId = new { root = "2.16.840.1.113883.10.20.22.4.16", extension = "2014-06-09" },
                        id = new { root = "6c844c75-aa34-411c-b7bd-5e4a9f206e29", },
                        statusCode = new { code = "completed" },
                        effectiveTime = new object[] {
                            new {
                                value = "202011071159-0700"
                            },
                            new Dictionary<string, object>
                            {
                                { "operator", "A" },
                                { "period", new { value = "12", unit = "h" } }
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
                },
            };
            var actualFhir = GetFhirObjectFromTemplate<MedicationAdministration>(ECRPath, attributes);

            string actualFhirString = JsonSerializer.Serialize(actualFhir);
            Assert.False(actualFhirString.Contains("Red herring"));

            Assert.Equal("MedicationAdministration", actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);
            Assert.NotEmpty(actualFhir.Identifier);
            Assert.NotNull(actualFhir.Status);
            Assert.NotEmpty(actualFhir.Effective);

            Assert.Equal("ORAL", actualFhir.Dosage.Route.Coding.First().Display);
            Assert.Equal(1, actualFhir.Dosage.Dose.Value);
            Assert.Equal("g", actualFhir.Dosage.Dose.Unit);
            var dosageRateQuantity = actualFhir.Dosage.Rate as Quantity;
            Assert.Equal(12, dosageRateQuantity.Value);
            Assert.Equal("h", dosageRateQuantity.Unit);

            Assert.Equal("Patient\u0027s condition improved", actualFhir.GetExtensionValue<CodeableConcept>("http://hl7.org/fhir/us/ecr/StructureDefinition/us-ph-therapeutic-medication-response-extension").Coding.First().Display);
        }
    }
}
