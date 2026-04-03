using System.Collections.Generic;
using System.IO;
using Hl7.Fhir.Model;
using Xunit;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests
{
    public class CodeableConceptTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "DataType",
            "CodeableConcept.liquid"
        );

        [Fact]
        public async System.Threading.Tasks.Task AllFieldsEmptyOptional()
        {
            var expectedContent = @"""coding"": [ ],";
            await ConvertCheckLiquidTemplate(ECRPath, new Dictionary<string, object>(), expectedContent);
        }

        [Fact]
        public async System.Threading.Tasks.Task MissingCodeButTextExtensible()
        {
            var attributes = new Dictionary<string, object>
            {
                {
                    "CodeableConcept",
                    new { nullFlavor = "OTH", originalText = new { _ = "Ship Name" } }
                }
            };
            var expectedContent = @"""coding"": [ ],""text"": ""Ship Name"",";
            await ConvertCheckLiquidTemplate(ECRPath, attributes, expectedContent);
        }

        [Fact]
        public async System.Threading.Tasks.Task AllFieldsEmptyRequired()
        {
            var attributes = new Dictionary<string, object>
            {
                { "CodeableConcept", new { nullFlavor = "UNK" }}
            };
            attributes.Add("minCardinality", 1);
            attributes.Add("bindingStrength", "required");

            var expectedContent =
                @"""coding"": [ ], ""extension"": [{ ""url"": ""http://hl7.org/fhir/StructureDefinition/data-absent-reason"", ""valueCode"": ""unknown"", },],";
            await ConvertCheckLiquidTemplate(ECRPath, attributes, expectedContent);
        }

        [Fact]
        public async System.Threading.Tasks.Task AllFieldsEmptyExtensible()
        {
            var attributes = new Dictionary<string, object>
            {
                { "CodeableConcept", new { nullFlavor = "UNK" }}
            };
            attributes.Add("minCardinality", 1);
            attributes.Add("bindingStrength", "extensible");

            var expectedContent =
                @"""coding"": [ { ""code"": ""unknown"", ""system"": ""http://terminology.hl7.org/CodeSystem/data-absent-reason"", },],";
            await ConvertCheckLiquidTemplate(ECRPath, attributes, expectedContent);
        }

        [Fact]
        public async System.Threading.Tasks.Task NullCodeWithTranslation()
        {
            var attributes = new Dictionary<string, object>
            {
                {
                    "CodeableConcept",
                    new
                    {
                        nullFlavor = "OTH",
                        originalText = new { reference = "immunization6Name" },
                        translation = new List<object>
                        {
                            new
                            {
                                code = "49281-400-10",
                                codeSystem = "2.16.840.1.113883.6.69",

                                codeSystemName = "NDC"
                            }
                        }
                    }
                }
            };
            var expectedContent =
                @"""coding"": [ { ""code"": ""49281-400-10"",""system"": ""urn:oid:2.16.840.1.113883.6.69"",""display"": """",},],";
            await ConvertCheckLiquidTemplate(ECRPath, attributes, expectedContent);
        }

        [Fact]
        public async System.Threading.Tasks.Task NullCodeWithSystemAndTranslation()
        {
            var attributes = new Dictionary<string, object>
            {
                {
                    "CodeableConcept",
                    new
                    {
                        codeSystem = "2.16.840.1.113883.6.88",
                        nullFlavor = "OTH",
                        translation = new List<object>
                        {
                            new
                            {
                                code = "410942007",
                                codeSystem = "2.16.840.1.113883.6.96",
                                codeSystemName = "SNOMED CT",
                                displayName = "drug or medication"
                            }
                        }
                    }
                }
            };
            var expectedContent =
                @"""coding"": [ { ""code"": """",""system"": ""http://www.nlm.nih.gov/research/umls/rxnorm"",""display"": """",}, { ""code"": ""410942007"",""system"": ""http://snomed.info/sct"",""display"": ""Drug or medicament"",},],";
            await ConvertCheckLiquidTemplate(ECRPath, attributes, expectedContent);
        }

        [Fact]
        public async System.Threading.Tasks.Task OriginalTextContainsSpecialCharacter()
        {
            var attributes = new Dictionary<string, object>
            {
                {
                    "CodeableConcept",
                    new { originalText = new { _ = @"Ship \ Name" } }
                }
            };
            
            // We need to make the output of the template into a complete JSON object and attempt to deserialize 
            // in order for this to fail if the implementation is not correct
            var actualFhir = await GetFhirObjectFromPartialTemplate<CodeableConcept>(ECRPath, attributes);
            Assert.Equal(actualFhir.Text, "Ship \\ Name");
        }
    }
}
