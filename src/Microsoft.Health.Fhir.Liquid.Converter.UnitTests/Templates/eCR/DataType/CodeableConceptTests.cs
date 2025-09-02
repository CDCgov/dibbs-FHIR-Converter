using System.Collections.Generic;
using System.IO;
using DotLiquid;
using Hl7.Fhir.Model;
using Xunit;

namespace Microsoft.Health.Fhir.Liquid.Converter.UnitTests
{
    public class CodeableConceptTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "DataType",
            "_CodeableConcept.liquid"
        );

        [Fact]
        public void AllFieldsEmptyOptional()
        {
            var expectedContent = @"""coding"": [ ], ""text"": """",";
            ConvertCheckLiquidTemplate(ECRPath, new Dictionary<string, object>(), expectedContent);
        }

        [Fact]
        public void MissingCodeButTextExtensible()
        {
            var attributes = new Dictionary<string, object>
            {
                {
                    "CodeableConcept",
                    Hash.FromAnonymousObject(
                        new { nullFlavor = "OTH", originalText = new { _ = "Ship Name" } }
                    )
                }
            };
            var expectedContent = @"""coding"": [ ], ""text"": ""Ship Name"",";
            ConvertCheckLiquidTemplate(ECRPath, attributes, expectedContent);
        }

        [Fact]
        public void AllFieldsEmptyRequired()
        {
            var attributes = new Dictionary<string, object>
            {
                { "CodeableConcept", Hash.FromAnonymousObject(new { nullFlavor = "UNK" }) }
            };
            attributes.Add("minCardinality", 1);
            attributes.Add("bindingStrength", "required");

            var expectedContent =
                @"""coding"": [ ], ""text"": """", ""extension"": [ { ""url"": ""http://hl7.org/fhir/StructureDefinition/data-absent-reason"", ""valueCode"": ""unknown"", },],";
            ConvertCheckLiquidTemplate(ECRPath, attributes, expectedContent);
        }

        [Fact]
        public void AllFieldsEmptyExtensible()
        {
            var attributes = new Dictionary<string, object>
            {
                { "CodeableConcept", Hash.FromAnonymousObject(new { nullFlavor = "UNK" }) }
            };
            attributes.Add("minCardinality", 1);
            attributes.Add("bindingStrength", "extensible");

            var expectedContent =
                @"""coding"": [ { ""code"": ""unknown"", ""system"": ""http://terminology.hl7.org/CodeSystem/data-absent-reason"", }, ], ""text"": """",";
            ConvertCheckLiquidTemplate(ECRPath, attributes, expectedContent);
        }

        [Fact]
        public void NullCodeWithTranslation()
        {
            var attributes = new Dictionary<string, object>
            {
                {
                    "CodeableConcept",
                    Hash.FromAnonymousObject(
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
                    )
                }
            };
            var expectedContent =
                @"""coding"": [ { ""code"": ""49281-400-10"", ""system"": ""urn:oid:2.16.840.1.113883.6.69"", }, ], ""text"": """",";
            ConvertCheckLiquidTemplate(ECRPath, attributes, expectedContent);
        }

        [Fact]
        public void NullCodeWithSystemAndTranslation()
        {
            var attributes = new Dictionary<string, object>
            {
                {
                    "CodeableConcept",
                    Hash.FromAnonymousObject(
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
                    )
                }
            };
            var expectedContent =
                @"""coding"": [ { ""code"": """", ""system"": ""http://www.nlm.nih.gov/research/umls/rxnorm"", ""display"": """", }, { ""code"": ""410942007"", ""system"": ""http://snomed.info/sct"", ""display"": ""drug or medication"", }, ], ""text"": """",";
            ConvertCheckLiquidTemplate(ECRPath, attributes, expectedContent);
        }
    }
}
