using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hl7.Fhir.Model;
using Microsoft.Health.Fhir.Liquid.Converter.Parsers;
using Xunit;

namespace Microsoft.Health.Fhir.Liquid.Converter.UnitTests
{
    public class CompositionTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "Resource",
            "_Composition.liquid"
        );

        private static Composition ActualFhir;

        public CompositionTests()
        {
            var xmlStr = File.ReadAllText("../../data/SampleData/eCR/eCR_RR_combined_special_chars.xml");
            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;
            var attributes = new Dictionary<string, object>
            {
                { "composition", parsed["ClinicalDocument"] }, 
                { "practitionerId", "1234" },
                { "ID", "5678" },
            };

            ActualFhir = GetFhirObjectFromTemplate<Composition>(ECRPath, attributes);
        }

        [Fact]
        public void EscapesSpecialCharactersInProblemObservationValueDisplayName()
        {
            var problemSection = ActualFhir.Section.FirstOrDefault(section => section.Title == "Active Problems");
            var problemEntry = problemSection.Entry.FirstOrDefault(entry => entry.Display.StartsWith("Problem - Cleft hard palate \\"));
            Assert.NotNull(problemEntry);
        }

        [Fact]
        public void EscapesSpecialCharactersInProblemObservationValueTranslationDisplayName()
        {
            var problemSection = ActualFhir.Section.FirstOrDefault(section => section.Title == "Active Problems");
            var problemEntry = problemSection.Entry.FirstOrDefault(entry => entry.Display.StartsWith("Problem - Zika \\ virus \\\\ disease"));
            Assert.NotNull(problemEntry);
        }

        [Fact]
        public void EscapesSpecialCharactersInResultsObservationCodeOriginalText()
        {
            var resultsSection = ActualFhir.Section.FirstOrDefault(section => section.Title == "Results");
            var resultsEntry = resultsSection.Entry.FirstOrDefault(entry => entry.Display == "\"ESCAPED\"");
            Assert.NotNull(resultsEntry);
        }

        [Fact]
        public void EscapesSpecialCharactersInResultsObservationCodeTranslationDisplayName()
        {
            var resultsSection = ActualFhir.Section.FirstOrDefault(section => section.Title == "Results");
            var resultsEntry = resultsSection.Entry.FirstOrDefault(entry => entry.Display == "\\Also escaped\\");
            Assert.NotNull(resultsEntry);
        }

        [Fact]
        public void EscapesSpecialCharactersInRRObservationValueDisplayName()
        {
            var rrSection = ActualFhir.Section.FirstOrDefault(section => section.Title == "Reportability Response Information Section");
            var rrEntry = rrSection.Entry.FirstOrDefault(entry => entry.Display == "Relevant Reportable Condition Observation - Cleft hard palate \\");
            Assert.NotNull(rrEntry);
        }
    }
}