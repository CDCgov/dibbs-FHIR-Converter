using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Hl7.Fhir.Model;
using Xunit;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests
{
    public class MedicationTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "Resource",
            "Medication.liquid"
        );

        [Fact]
        public void Medication_AllFields()
        {
            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "medicationStatus", "active" },
                {
                    "medication",
                    new {
                        code = new {
                            code = "code-code-test",
                            codeSystem = "code-codeSystem-test",
                            codeSystemName = "RxNorm",
                            displayName = "medication-name-test"
                        }
                    }
                },
            };
            var actualFhir = GetFhirObjectFromTemplate<Medication>(ECRPath, attributes);

            Assert.Equal("Medication", actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);
            Assert.Equal(Medication.MedicationStatusCodes.Active, actualFhir.Status);
            var coding = actualFhir.Code.Coding.First();
            Assert.Equal("code-code-test", coding.Code);
            Assert.Equal("code-codeSystem-test", coding.System);
            Assert.Equal("medication-name-test", coding.Display);
        }
    }
}
