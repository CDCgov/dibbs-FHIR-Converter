using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Hl7.Fhir.Model;
using Xunit;
using Dibbs.Fhir.Liquid.Converter.DataParsers;

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
                        text = new { _ = "1 tablet oral" },
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

            Assert.Equal("1 tablet oral", actualFhir.Dosage.Text);
            Assert.Equal("ORAL", actualFhir.Dosage.Route.Coding.First().Display);
            Assert.Equal(1, actualFhir.Dosage.Dose.Value);
            Assert.Equal("g", actualFhir.Dosage.Dose.Unit);
            var dosageRateQuantity = actualFhir.Dosage.Rate as Quantity;
            Assert.Equal(12, dosageRateQuantity.Value);
            Assert.Equal("h", dosageRateQuantity.Unit);

            Assert.Equal("Patient\u0027s condition improved", actualFhir.GetExtensionValue<CodeableConcept>("http://hl7.org/fhir/us/ecr/StructureDefinition/us-ph-therapeutic-medication-response-extension").Coding.First().Display);
        }

        [Fact]
        public void MedicationAdministration_DosageText_WithInnerText()
        {
            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                {
                    "medicationAdministration",
                    new
                    {
                        statusCode = new { code = "completed" },
                        text = new { _ = "Take 1 tablet orally & daily" },
                    }
                },
            };
            var actualFhir = GetFhirObjectFromTemplate<MedicationAdministration>(ECRPath, attributes);

            Assert.Equal("Take 1 tablet orally & daily", actualFhir.Dosage.Text);
        }

        [Fact]
        public void MedicationAdministration_DosageText_WithSectionReference()
        {
            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                {
                    "medicationAdministration",
                    new
                    {
                        statusCode = new { code = "completed" },
                        text = new { reference = new { _ = "1 tablet oral twice daily", value = "#sig1" } },
                    }
                },
            };
            var actualFhir = GetFhirObjectFromTemplate<MedicationAdministration>(ECRPath, attributes);

            Assert.Equal("1 tablet oral twice daily", actualFhir.Dosage.Text);
        }

        [Fact]
        public void MedicationAdministration_DosageText_WithSimpleText()
        {
            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                {
                    "medicationAdministration",
                    new
                    {
                        statusCode = new { code = "completed" },
                        text = new { _ = "Take 2 tablets every 8 hours" },
                    }
                },
            };
            var actualFhir = GetFhirObjectFromTemplate<MedicationAdministration>(ECRPath, attributes);

            Assert.Equal("Take 2 tablets every 8 hours", actualFhir.Dosage.Text);
        }

        [Fact]
        public void PregnancyMedicationAdministration_AllFields()
        {
            var xmlString =
                @"
                <substanceAdministration classCode=""SBADM"" moodCode=""EVN"">
                    <!-- [C-CDA R2.1] Medication Activity (V2) -->
                    <templateId root=""2.16.840.1.113883.10.20.22.4.16"" extension=""2014-06-09"" />
                    <!-- [C-CDA PREG] D Immune Globulin (RhIG) Given -->
                    <templateId root=""2.16.840.1.113883.10.20.22.4.302"" extension=""2018-04-01"" />
                    <id root=""7abe5aa2-c1ba-4552-85fc-796b4c00c50d"" />
                    <statusCode code=""completed"" />
                    <effectiveTime xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""IVL_TS"">
                        <low value=""20170618"" />
                    </effectiveTime>
                    <consumable>
                        <manufacturedProduct classCode=""MANU"">
                            <!-- [C-CDA R2.1] Medication information -->
                            <templateId root=""2.16.840.1.113883.10.20.22.4.23"" extension=""2014-06-09"" />
                            <!-- [C-CDA PREG] D Immune Globulin (RhIG) -->
                            <templateId root=""2.16.840.1.113883.10.20.22.4.303"" extension=""2018-04-01"" />
                            <id root=""8bdb8496-2acb-4ae3-b231-69e4c92e25a1"" />
                            <manufacturedMaterial>
                                <code code=""1790513""
                                    displayName=""13 ML Rho(D) Immune Globulin, human 1154 UNT/ML Injection [WinRho]""
                                    codeSystem=""2.16.840.1.113883.6.88""
                                    codeSystemName=""RxNorm"" />
                            </manufacturedMaterial>
                        </manufacturedProduct>
                    </consumable>
                </substanceAdministration>";

            var parser = new CcdaDataParser();
            var parsedXml = parser.Parse(xmlString) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "medicationAdministration", parsedXml["substanceAdministration"] },
            };

            var actualFhir = GetFhirObjectFromTemplate<MedicationAdministration>(ECRPath, attributes);

            Assert.Equal("MedicationAdministration", actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);
            Assert.NotEmpty(actualFhir.Identifier);
            Assert.Equal("Completed", actualFhir.Status.ToString());

            Assert.Equal("2017-06-18", (actualFhir.Effective as Period)?.Start);
        }
    }
}
