using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests
{
    public class PatientProficiencyTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory, "Extension", "PatientProficiency.liquid");

        [Fact]
        public async System.Threading.Tasks.Task GivenNoAttributeReturnsEmpty()
        {
            await ConvertCheckLiquidTemplate(ECRPath, new Dictionary<string, object>(), string.Empty);
        }

        [Fact]
        public async System.Threading.Tasks.Task GivenModeCodeAndProficiencyLevelCodeReturnsBothInPatientProficiency()
        {
            var attributes = new Dictionary<string, object>
            {
                {
                    "modeCode", new Dictionary<string, string>
                    {
                        { "code", "esp" },
                    }
                },
                {
                    "proficiencyLevelCode", new Dictionary<string, string>
                    {
                        { "code", "e" },
                    }
                },
            };

            await ConvertCheckLiquidTemplate(
                ECRPath,
                attributes,
                @"""url"": ""http://hl7.org/fhir/StructureDefinition/patient-proficiency"", ""extension"": [ { ""url"": ""type"", ""valueCoding"": { ""system"": ""http://terminology.hl7.org/CodeSystem/v3-LanguageAbilityMode"", ""code"": ""ESP"", ""display"": ""Expressed spoken"", }, }, { ""url"": ""level"", ""valueCoding"": { ""system"": ""http://terminology.hl7.org/CodeSystem/v3-LanguageAbilityProficiency"", ""code"": ""E"", ""display"": ""Excellent"", }, }, ],");
        }

        [Fact]
        public async System.Threading.Tasks.Task GivenModeCodeReturnsModeCodeInPatientProficiency()
        {
            var attributes = new Dictionary<string, object>
            {
                {
                    "modeCode", new Dictionary<string, string>
                    {
                        { "code", "esp" },
                    }
                },
            };

            await ConvertCheckLiquidTemplate(
                ECRPath,
                attributes,
                @"""url"": ""http://hl7.org/fhir/StructureDefinition/patient-proficiency"", ""extension"": [ { ""url"": ""type"", ""valueCoding"": { ""system"": ""http://terminology.hl7.org/CodeSystem/v3-LanguageAbilityMode"", ""code"": ""ESP"", ""display"": ""Expressed spoken"", }, }, ],");
        }

        [Fact]
        public async System.Threading.Tasks.Task GivenProficiencyLevelCodeReturnsProficiencyLevelCodeInPatientProficiency()
        {
            var attributes = new Dictionary<string, object>
            {
                {
                    "proficiencyLevelCode", new Dictionary<string, string>
                    {
                        { "code", "e" },
                    }
                },
            };

            await ConvertCheckLiquidTemplate(
                ECRPath,
                attributes,
                @"""url"": ""http://hl7.org/fhir/StructureDefinition/patient-proficiency"", ""extension"": [ { ""url"": ""level"", ""valueCoding"": { ""system"": ""http://terminology.hl7.org/CodeSystem/v3-LanguageAbilityProficiency"", ""code"": ""E"", ""display"": ""Excellent"", }, }, ],");
        }

        [Fact]
        public async System.Threading.Tasks.Task GivenInvalidProficiencyLevelCodeReturnsNothing()
        {
            var attributes = new Dictionary<string, object>
            {
                {
                    "proficiencyLevelCode", new Dictionary<string, string>
                    {
                        { "code", "na" },
                    }
                },
            };

            await ConvertCheckLiquidTemplate(
                ECRPath,
                attributes,
                string.Empty);
        }

        [Fact]
        public async System.Threading.Tasks.Task GivenInvalidModeCodeReturnsNothing()
        {
            var attributes = new Dictionary<string, object>
        {
            {
                "modeCode", new Dictionary<string, string>
                {
                    { "code", "na" },
                }
            },
        };

            await ConvertCheckLiquidTemplate(
                ECRPath,
                attributes,
                string.Empty);
        }
    }
}
