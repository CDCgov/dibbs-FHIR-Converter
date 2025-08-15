using System.Collections.Generic;
using System.IO;
using DotLiquid;
using Hl7.Fhir.Model;
using Microsoft.Health.Fhir.Liquid.Converter.Parsers;
using Xunit;

namespace Microsoft.Health.Fhir.Liquid.Converter.UnitTests
{
    /// <summary>
    /// Test for the Identifier template. These are based on the cases listed here: https://build.fhir.org/ig/HL7/ccda-on-fhir/mappingGuidance.html#cda-id--fhir-identifier
    /// </summary>
    public class IdentifierTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "DataType",
            "_Identifier.liquid"
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
            Assert.Empty(actualFhir.Assigner.Display);
        }

        /// <summary>
        /// When we know the corresponding URL for a root OID, the URL should be used as the Identifier's system.
        /// </summary>
        [Fact]
        public void RootAndExtensionUrlExists()
        {
            var xmlStr = @"<id extension=""77777777777"" root=""2.16.840.1.113883.4.6"" />";
            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object> { { "Identifier", parsed["id"] }, };

            var actualFhir = GetFhirObjectFromTemplate<Identifier>(ECRPath, attributes);

            Assert.Equal("http://hl7.org/fhir/sid/us-npi", actualFhir.System);
            Assert.Equal("77777777777", actualFhir.Value);
        }

        /// <summary>
        /// If we do not know the corresponding URL for an OID root, it should be converted to a URI, by appending ":urn:oid"
        /// </summary>
        [Fact]
        public void RootAndExtensionNoUrlExists()
        {
            var xmlStr = @"<id extension=""12345V7890"" root=""1.2.3.4"" />";
            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object> { { "Identifier", parsed["id"] }, };

            var actualFhir = GetFhirObjectFromTemplate<Identifier>(ECRPath, attributes);

            Assert.Equal("urn:oid:1.2.3.4", actualFhir.System);
            Assert.Equal("12345V7890", actualFhir.Value);
        }

        /// <summary>
        /// If no corresponding URL exists for the root OID, and there is not extension, "urn:ietf:rfc:3986" is the Identifier system, and the prepended OID is the value.
        /// </summary>
        [Fact]
        public void NoUrlExistsAndNoExtension()
        {
            var xmlStr = @"<id root=""2.16.840.1.113883.3.72.5.20"" />";
            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object> { { "Identifier", parsed["id"] }, };

            var actualFhir = GetFhirObjectFromTemplate<Identifier>(ECRPath, attributes);

            Assert.Equal("urn:ietf:rfc:3986", actualFhir.System);
            Assert.Equal("urn:oid:2.16.840.1.113883.3.72.5.20", actualFhir.Value);
        }

        /// <summary>
        /// If the root is an UUID, prepend with "urn:uuid".
        /// </summary>
        [Fact]
        public void UuidOnly()
        {
            var xmlStr = @"<id root=""6c844c75-aa34-411c-b7bd-5e4a9f206e29"" />";
            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object> { { "Identifier", parsed["id"] }, };

            var actualFhir = GetFhirObjectFromTemplate<Identifier>(ECRPath, attributes);

            Assert.Equal("urn:ietf:rfc:3986", actualFhir.System);
            Assert.Equal("urn:uuid:6c844c75-aa34-411c-b7bd-5e4a9f206e29", actualFhir.Value);
        }

        /// <summary>
        /// If the root is neither an OID nor an UUID, just set it as the value of the Identifier.
        /// </summary>
        [Fact]
        public void InvalidUidOnly()
        {
            var xmlStr = @"<id root=""7c0704bb-9c40-41b5-9c7d-26b2d59e234g"" />";
            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object> { { "Identifier", parsed["id"] }, };

            var actualFhir = GetFhirObjectFromTemplate<Identifier>(ECRPath, attributes);

            Assert.Equal("7c0704bb-9c40-41b5-9c7d-26b2d59e234g", actualFhir.Value);
            Assert.Null(actualFhir.System);
        }

        /// <summary>
        /// If the root is an UUID and there is an extension. Prepend the UUID and use that as the system, the extension becomes the Value.
        /// </summary>
        [Fact]
        public void UuidWithExtension()
        {
            var xmlStr =
                @"<id root=""58822180-ab0d-42e4-90c6-35336bf55654"" extension=""12345"" />";
            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object> { { "Identifier", parsed["id"] }, };

            var actualFhir = GetFhirObjectFromTemplate<Identifier>(ECRPath, attributes);

            Assert.Equal("urn:uuid:58822180-ab0d-42e4-90c6-35336bf55654", actualFhir.System);
            Assert.Equal("12345", actualFhir.Value);
        }

        /// <summary>
        /// If there is only an extension on the ID, use that as the Identifier value.
        /// </summary>
        [Fact]
        public void ExtensionOnly()
        {
            var xmlStr = @"<id extension=""12345"" />";
            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object> { { "Identifier", parsed["id"] }, };

            var actualFhir = GetFhirObjectFromTemplate<Identifier>(ECRPath, attributes);

            Assert.Empty(actualFhir.System);
            Assert.Equal("12345", actualFhir.Value);
        }

        /// <summary>
        /// If the Root is an OID with a corresponding URL, and the extension begins with that URL, remove the root portion from the extension.
        /// </summary>
        [Fact]
        public void RootUriValueURL()
        {
            var xmlStr =
                @"<id root=""2.16.840.1.113883.6.12"" extension=""http://www.ama-assn.org/go/cpt/1234"" />";
            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object> { { "Identifier", parsed["id"] }, };

            var actualFhir = GetFhirObjectFromTemplate<Identifier>(ECRPath, attributes);

            Assert.Equal("http://www.ama-assn.org/go/cpt", actualFhir.System);
            Assert.Equal("1234", actualFhir.Value);
        }
    }
}
