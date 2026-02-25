using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hl7.Fhir.Model;
using Xunit;
using System.Text.Json;
using System.Reflection;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests
{
    public class MedicationStatementTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "Resource",
            "MedicationStatement.liquid"
        );

        [Fact]
        public void MedicationStatement_AllFields()
        {
            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                {
                    "medicationStatement",
                    new {
                            id = new { root = "test-id", },
                            statusCode = new { code = "active" },
                            text = new { _ = "medication instructions" },
                            effectiveTime = new {
                                "xsi:type" = "IVL_TS",
                                low = new {
                                    value = "effectiveTime-low"
                                },
                                high = new {
                                    value = "effectiveTime-low"
                                },
                            },
                            // author = new {
                            //     time = "20201030"
                            // },
                            // approachSiteCode = new { code = "Abdomen" },
                            // routeCode = new {
                            //     code = "a code",
                            //     translation = new { code = "Synonym" }
                            // },
                            // doseQuantity = new { value = "3", unit = "mg" },
                            // repeatNumber = new { value = "1" },
                        }
                },
            };
            var actualFhir = GetFhirObjectFromTemplate<MedicationStatement>(ECRPath, attributes);

            Console.WriteLine(JsonSerializer.Serialize(actualFhir, new JsonSerializerOptions
            {
                WriteIndented = true,
            }));

            Assert.Equal("MedicationStatement", actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);
            Assert.Equal(MedicationStatement.MedicationStatusCodes.Active, actualFhir.Status);
            // Assert.Equal("Why not", actualFhir.ReasonCode.First().Coding.First().Code);
            // Assert.Equal(RequestPriority.Asap , actualFhir.Priority);
            // Assert.Equal(1, actualFhir.DispenseRequest.NumberOfRepeatsAllowed);
            // var dosage = actualFhir.DosageInstruction.First();
            // Assert.NotEmpty(dosage.Site);
            // Assert.Equal("Abdomen", dosage.Site.Coding.First().Code);
            // Assert.NotEmpty(dosage.DoseAndRate);
            // Assert.NotEmpty(dosage.Route);
        }
    }
}
