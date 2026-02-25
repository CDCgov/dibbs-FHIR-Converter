using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hl7.Fhir.Model;
using Xunit;
using Dibbs.Fhir.Liquid.Converter.DataParsers;
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
            var xmlStr = @"
                <substanceAdministration classCode=""SBADM"" moodCode=""INT"">
                    <templateId root=""id-test""/>
                    <text>Medication instructions</text>
                    <statusCode code=""active""/>
                    <effectiveTime xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""IVL_TS"">
                        <low value=""20260201""/>
                        <high value=""20260228""/>
                    </effectiveTime>
                    <effectiveTime xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" institutionSpecified=""true"" operator=""A"" xsi:type=""PIVL_TS"">
                        <period unit=""d"" value=""1""/>
                    </effectiveTime>
                    <routeCode code=""C38288"" codeSystem=""2.16.840.1.113883.3.26.1.1"" codeSystemName=""NCI Thesaurus"" displayName=""oral"">
                        <originalText>oral</originalText>
                    </routeCode>
                    <doseQuantity unit=""mg"" value=""5""/>
                    <author>
                        <time value=""20260201-0500""/>
                    </author>
                </substanceAdministration>";

            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "medicationStatement", parsed["substanceAdministration"]},
            };

            var actualFhir = GetFhirObjectFromTemplate<MedicationStatement>(ECRPath, attributes);

            Console.WriteLine(JsonSerializer.Serialize(actualFhir, new JsonSerializerOptions
            {
                WriteIndented = true,
            }));

            Assert.Equal("MedicationStatement", actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);
            Assert.Equal(MedicationStatement.MedicationStatusCodes.Active, actualFhir.Status);
            Assert.Equal("2026-02-01", (actualFhir.Effective as Period)?.Start);
            Assert.Equal("2026-02-28", (actualFhir.Effective as Period)?.End);
            Assert.Equal("2026-02-01", actualFhir.DateAsserted);
            Assert.Equal("Medication instructions", actualFhir.Dosage.First().Text);
            Assert.Equal(1.0m, actualFhir.Dosage.First().Timing.Repeat.Period.Value);
            Assert.Equal(Hl7.Fhir.Model.Timing.UnitsOfTime.D, actualFhir.Dosage.First().Timing.Repeat.PeriodUnit);
            var routeCoding = actualFhir.Dosage.First().Route.Coding.First();
            Assert.Equal("oral", routeCoding.Display);
            var doseAndRate = actualFhir.Dosage.First().DoseAndRate.First();
            Assert.Equal(5, (doseAndRate.Dose as Quantity)?.Value);
            Assert.Equal("mg", (doseAndRate.Dose as Quantity)?.Unit);
        }
    }
}
