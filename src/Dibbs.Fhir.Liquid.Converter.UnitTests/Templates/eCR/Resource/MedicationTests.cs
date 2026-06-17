using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Hl7.Fhir.Model;
using Xunit;
using Dibbs.Fhir.Liquid.Converter.DataParsers;

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

        [Fact]
        public void PregnancyMedication_AllFields()
        {
            var xmlString =
                @"
                <manufacturedMaterial>
                    <code code=""1790513""
                        displayName=""13 ML Rho(D) Immune Globulin, human 1154 UNT/ML Injection [WinRho]""
                        codeSystem=""2.16.840.1.113883.6.88""
                        codeSystemName=""RxNorm"" />
                </manufacturedMaterial>";

            var parser = new CcdaDataParser();
            var parsedXml = parser.Parse(xmlString) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "medication", parsedXml["manufacturedMaterial"] },
            };

            var actualFhir = GetFhirObjectFromTemplate<Medication>(ECRPath, attributes);

            Assert.Equal("Medication", actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);

            var coding = actualFhir.Code.Coding.First();
            Assert.Equal("1790513", coding.Code);
            Assert.Equal("http://www.nlm.nih.gov/research/umls/rxnorm", coding.System);
            Assert.Equal("13 ML Rho(D) immune globulin, human 1154 UNT/ML Injection [WinRho]", coding.Display);
        }
    }
}
