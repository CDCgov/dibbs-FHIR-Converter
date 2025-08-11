using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotLiquid;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Health.Fhir.Liquid.Converter.Parsers;
using Xunit;

namespace Microsoft.Health.Fhir.Liquid.Converter.UnitTests
{
    public class SystemReferenceTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "ValueSet",
            "_SystemReference.liquid"
        );

        [Fact]
        public void known_oid()
        {
            // SNOMED
            var systemOid = "2.16.840.1.113883.3.88.12.3221.8.9";
            var expectedValue = "http://snomed.info/sct";

            var attributes = new Dictionary<string, object> { { "code", systemOid }, };

            ConvertCheckLiquidTemplate(ECRPath, attributes, expectedValue);
        }

        [Fact]
        public void unknown_oid()
        {
            // SNOMED
            var systemOid = "1.2.3.4";
            var expectedValue = "urn:oid:" + systemOid;

            var attributes = new Dictionary<string, object> { { "code", systemOid }, };

            ConvertCheckLiquidTemplate(ECRPath, attributes, expectedValue);
        }

        [Fact]
        public void not_a_oid()
        {
            // SNOMED
            var code = "not a oid";

            var attributes = new Dictionary<string, object> { { "code", code }, };

            ConvertCheckLiquidTemplate(ECRPath, attributes, "");
        }
    }
}
