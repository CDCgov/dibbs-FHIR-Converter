using System.Collections.Generic;
using System.IO;
using DotLiquid;
using Hl7.Fhir.Model;
using Microsoft.Health.Fhir.Liquid.Converter.Parsers;
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
            var attributes = new Dictionary<string, object> { { "Coding", "" }, };

            var coding = GetFhirObjectFromTemplate<Coding>(ECRPath, attributes);

            Assert.Empty(coding.System);
            Assert.Empty(coding.Code);
        }

        [Fact]
        public void AllFields()
        {
            var xmlString =
                @"
            <code
                xmlns:sdtc=""urn:hl7-org:sdtc""
                code=""385857005""
                codeSystem=""2.16.840.1.113883.6.96""
                codeSystemName=""SNOMED CT""
                displayName=""Ventilator care and adjustment (regime/therapy)""
                sdtc:valueSet=""2.16.840.1.114222.4.11.7508""
                sdtc:valueSetVersion=""2020-11-13"" />
            ";
            var parsed = new CcdaDataParser().Parse(xmlString) as Dictionary<string, object>;
            var attributes = new Dictionary<string, object> { { "Coding", parsed["code"] }, };

            var coding = GetFhirObjectFromTemplate<Coding>(ECRPath, attributes);

            Assert.Equal("http://snomed.info/sct", coding.System);
            Assert.Equal("385857005", coding.Code);
            Assert.Equal("Ventilator care and adjustment (regime/therapy)", coding.Display);
        }
    }
}
