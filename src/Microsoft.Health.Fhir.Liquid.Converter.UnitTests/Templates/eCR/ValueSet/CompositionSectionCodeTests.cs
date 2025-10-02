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
    public class CompositionSectionCodeTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "ValueSet",
            "_CompositionSectionCode.liquid"
        );

        [Fact]
        public void KnownOid()
        {
            var systemOid = "1.3.6.1.4.1.19376.1.5.3.1.1.13.2.1";
            var expectedValue = @"""code"": ""10154-3"", ""display"": ""Chief complaint Narrative - Reported"",";

            var attributes = new Dictionary<string, object> { { "id", systemOid }, };

            ConvertCheckLiquidTemplate(ECRPath, attributes, expectedValue);
        }

        [Fact]
        public void UnknownOid()
        {
            var systemOid = "1.2.3.4";
            var expectedValue = "";

            var attributes = new Dictionary<string, object> { { "id", systemOid }, };

            ConvertCheckLiquidTemplate(ECRPath, attributes, expectedValue);
        }

        [Fact]
        public void NotAnOid()
        {
            var code = "not a oid";
            var expectedValue = "";

            var attributes = new Dictionary<string, object> { { "id", code }, };

            ConvertCheckLiquidTemplate(ECRPath, attributes, expectedValue);
        }
    }
}
