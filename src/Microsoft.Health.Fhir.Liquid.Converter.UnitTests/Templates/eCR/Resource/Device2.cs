using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotLiquid;
using Hl7.Fhir.Model;
using Microsoft.Health.Fhir.Liquid.Converter.Parsers;
using Xunit;

namespace Microsoft.Health.Fhir.Liquid.Converter.UnitTests
{
    public class Device2Tests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "Resource",
            "_Device2.liquid"
        );

        [Fact]
        public void Device2_CodeInRoot()
        {
            var xmlStr = @"
            <supply classCode=""SPLY"" moodCode=""EVN"">
                <id root=""cf75f5be-1da0-4256-8276-94b7fc73f9f9"" />
                <code code=""87405001"" displayName=""cane"" codeSystem=""2.16.840.1.113883.6.96""
                    codeSystemName=""SNOMED CT"">
                    <originalText>Upper GI Prosthesis</originalText>
                </code>
                <participant typeCode=""PRD"">
                    <participantRole classCode=""MANU"">
                    <scopingEntity>
                        <desc>""Good Health Durable Medical Equipment""</desc>
                    </scopingEntity>
                    </participantRole>
                </participant>
            </supply>
            ";

            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;
            var attributes = new Dictionary<string, object>
            {
                { "deviceEntry", parsed["supply"]},
                { "ID", "1234" },
            };

            var actualFhir = GetFhirObjectFromTemplate<Device>(ECRPath, attributes);

            Assert.Equal(ResourceType.Device.ToString(), actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);
            Assert.NotEmpty(actualFhir.Identifier);
            Assert.Equal("87405001", actualFhir.Type.Coding.First().Code);
            Assert.Equal("http://snomed.info/sct", actualFhir.Type.Coding.First().System);
            Assert.Equal("Cane", actualFhir.Type.Coding.First().Display);
            Assert.Equal("\"Good Health Durable Medical Equipment\"", actualFhir.Manufacturer);
            Assert.Equal("", actualFhir.DeviceName.First().Name);
        }

        [Fact]
        public void Device2_CodeInPlayingDevice()
        {
            var xmlStr = @"
            <supply classCode=""SPLY"" moodCode=""EVN"">
              <id root=""cf75f5be-1da0-4256-8276-94b7fc73f9f9"" />
              <participant typeCode=""PRD"">
                <participantRole classCode=""MANU"">
                  <playingDevice>
                    <code code=""87405001"" displayName=""cane"" codeSystem=""2.16.840.1.113883.6.96""
                      codeSystemName=""SNOMED CT"">
                      <originalText>Upper GI Prosthesis</originalText>
                    </code>
                  </playingDevice>
                  <scopingEntity>
                    <desc>""Good Health Durable Medical Equipment""</desc>
                  </scopingEntity>
                </participantRole>
              </participant>
            </supply>
            ";

            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;
            var attributes = new Dictionary<string, object>
            {
                { "deviceEntry", parsed["supply"]},
                { "ID", "1234" },
            };

            var actualFhir = GetFhirObjectFromTemplate<Device>(ECRPath, attributes);

            Assert.Equal(ResourceType.Device.ToString(), actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);
            Assert.NotEmpty(actualFhir.Identifier);
            Assert.Equal("87405001", actualFhir.Type.Coding.First().Code);
            Assert.Equal("http://snomed.info/sct", actualFhir.Type.Coding.First().System);
            Assert.Equal("Cane", actualFhir.Type.Coding.First().Display);
            Assert.Equal("\"Good Health Durable Medical Equipment\"", actualFhir.Manufacturer);
            Assert.Equal("", actualFhir.DeviceName.First().Name);
        }

        [Fact]
        public void Device2_DeviceNameOnly()
        {
            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "deviceName", "cane" },
            };

            var actualFhir = GetFhirObjectFromTemplate<Device>(ECRPath, attributes);

            Assert.Equal(ResourceType.Device.ToString(), actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);
            Assert.Empty(actualFhir.Identifier);
            Assert.Empty(actualFhir.Type.Coding);
            Assert.Equal("", actualFhir.Manufacturer);
            Assert.Equal("cane", actualFhir.DeviceName.First().Name);
        }
    }
}
