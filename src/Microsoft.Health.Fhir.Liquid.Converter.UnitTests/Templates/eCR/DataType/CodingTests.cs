using System.Collections.Generic;
using System.IO;
using DotLiquid;
using Xunit;

namespace Microsoft.Health.Fhir.Liquid.Converter.UnitTests
{
    public class CodingTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "DataType",
            "_Coding.liquid"
        );

        [Fact]
        public void GivenNoAttributeReturnsEmpty()
        {
            var expectedContent = @"""code"": """", ""system"": """", ""display"": """",";
            ConvertCheckLiquidTemplate(ECRPath, new Dictionary<string, object>(), expectedContent);
        }

        [Fact]
        public void AllFields()
        {
            var attributes = new Dictionary<string, object>
            {
                {
                    "Coding",
                    Hash.FromAnonymousObject(
                        new
                        {
                            code = "55751-2",
                            codeSystem = "2.16.840.1.113883.6.1",
                            displayName = "Public Health Case Report",
                            codeSystemName = "LOINC"
                        }
                    )
                }
            };
            var expectedContent =
                @"""code"": ""55751-2"", ""system"": ""http://loinc.org"", ""display"": ""Public health Case report"",";
            ConvertCheckLiquidTemplate(ECRPath, attributes, expectedContent);
        }

        [Fact]
        public void NoDisplay()
        {
            var attributes = new Dictionary<string, object>
            {
                {
                    "Coding",
                    Hash.FromAnonymousObject(
                        new { code = "2106-3", codeSystem = "2.16.840.1.113883.6.238" }
                    )
                }
            };
            var expectedContent =
                @"""code"": ""2106-3"", ""system"": ""urn:oid:2.16.840.1.113883.6.238"", ""display"": """",";
            ConvertCheckLiquidTemplate(ECRPath, attributes, expectedContent);
        }

        [Fact]
        public void UnknownLoincCode()
        {
            var attributes = new Dictionary<string, object>
            {
                {
                    "Coding",
                    Hash.FromAnonymousObject(
                        new
                        {
                            code = "TEST",
                            codeSystem = "http://loinc.org",
                            displayName = "Test Display Name"
                        }
                    )
                }
            };
            var expectedContent =
                @"""code"": ""TEST"", ""system"": ""http://loinc.org"", ""display"": ""Test Display Name"",";
            ConvertCheckLiquidTemplate(ECRPath, attributes, expectedContent);
        }

        [Fact]
        public void SnomedCode()
        {
            var attributes = new Dictionary<string, object>
            {
                {
                    "Coding",
                    Hash.FromAnonymousObject(
                        new
                        {
                            code = "1000004",
                            displayName = "Test",
                            codeSystem = "2.16.840.1.113883.6.96",
                            codeSystemName = "SNOMED CT",
                        }
                    )
                }
            };
            var expectedContent =
                @"""code"": ""1000004"", ""system"": ""http://snomed.info/sct"", ""display"": ""Sprain"",";
            ConvertCheckLiquidTemplate(ECRPath, attributes, expectedContent);
        }
    }
}
