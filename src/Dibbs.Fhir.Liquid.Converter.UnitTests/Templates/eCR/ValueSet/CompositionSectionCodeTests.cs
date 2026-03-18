using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Dibbs.Fhir.Liquid.Converter.DataParsers;
using Xunit;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests
{
    public class CompositionSectionCodeTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "ValueSet",
            "CompositionSectionCode.liquid"
        );

        [Fact]
        public async System.Threading.Tasks.Task KnownOid()
        {
            var systemOid = "1.3.6.1.4.1.19376.1.5.3.1.1.13.2.1";
            var expectedValue = @"""code"": ""10154-3"", ""display"": ""Chief complaint Narrative - Reported"",";

            var attributes = new Dictionary<string, object> { { "id", systemOid }, };

            await ConvertCheckLiquidTemplate(ECRPath, attributes, expectedValue);
        }

        [Fact]
        public async System.Threading.Tasks.Task UnknownOid()
        {
            var systemOid = "1.2.3.4";
            var expectedValue = "";

            var attributes = new Dictionary<string, object> { { "id", systemOid }, };

            await ConvertCheckLiquidTemplate(ECRPath, attributes, expectedValue);
        }

        [Fact]
        public async System.Threading.Tasks.Task NotAnOid()
        {
            var code = "not a oid";
            var expectedValue = "";

            var attributes = new Dictionary<string, object> { { "id", code }, };

            await ConvertCheckLiquidTemplate(ECRPath, attributes, expectedValue);
        }
    }
}
