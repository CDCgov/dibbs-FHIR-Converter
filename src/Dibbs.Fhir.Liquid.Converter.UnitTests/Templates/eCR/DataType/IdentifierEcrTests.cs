using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hl7.Fhir.Model;
using Dibbs.Fhir.Liquid.Converter.DataParsers;
using Xunit;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests
{
    /// <summary>
    /// Test for the Identifier ECR template.
    /// </summary>
    public class IdentifierEcrTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "DataType",
            "IdentifierEcr.liquid"
        );

        [Fact]
        public void GivenNoAttributeReturnsEmpty()
        {
            var actualFhir = GetFhirObjectFromTemplate<Identifier>(
                ECRPath,
                new Dictionary<string, object>()
            );

            Assert.Null(actualFhir.Value);
            Assert.Null(actualFhir.System);
            Assert.Null(actualFhir.Assigner);
        }

        /// <summary>
        /// System should be the root with the right prefix and value should be the extension
        /// </summary>
        [Fact]
        public void RootAndExtensionExists()
        {
            var xmlStr = @"<id extension=""77777777777"" root=""2.16.840.1.113883.4.6"" />";
            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object> { { "Identifier", parsed["id"] }, };

            var actualFhir = GetFhirObjectFromTemplate<Identifier>(ECRPath, attributes);

            Assert.Equal("urn:oid:2.16.840.1.113883.4.6", actualFhir.System);
            Assert.Equal("77777777777", actualFhir.Value);
        }

        /// <summary>
        /// System should be the uniform resource identifier and value should be the root with the correct prefix
        /// </summary>
        [Fact]
        public void OnlyRootExists()
        {
            var xmlStr = @"<id root=""2.16.840.1.113883.4.6"" />";
            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object> { { "Identifier", parsed["id"] }, };

            var actualFhir = GetFhirObjectFromTemplate<Identifier>(ECRPath, attributes);

            Assert.Equal("urn:ietf:rfc:3986", actualFhir.System);
            Assert.Equal("urn:oid:2.16.840.1.113883.4.6", actualFhir.Value);
        }

        /// <summary>
        /// System is null flavor and value is the extension
        /// </summary>
        [Fact]
        public void OnlyExtensionExists()
        {
            var xmlStr = @"<id extension=""77777777777"" />";
            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object> { { "Identifier", parsed["id"] }, };

            var actualFhir = GetFhirObjectFromTemplate<Identifier>(ECRPath, attributes);

            Assert.Equal("http://terminology.hl7.org/CodeSystem/v3-NullFlavor", actualFhir.System);
            Assert.Equal("77777777777", actualFhir.Value);
            Assert.Equal(
                "http://hl7.org/fhir/StructureDefinition/data-absent-reason",
                actualFhir.SystemElement.Extension.First().Url
            );
            Assert.Equal("unknown", actualFhir.SystemElement.Extension.First().Value.ToString());            
        }
    }
}
