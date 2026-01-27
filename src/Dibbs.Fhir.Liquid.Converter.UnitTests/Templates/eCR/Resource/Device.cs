using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hl7.Fhir.Model;
using Xunit;
using Dibbs.Fhir.Liquid.Converter.DataParsers;
using System;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests
{
    public class DeviceTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "Resource",
            "Device.liquid"
        );

        [Fact]
        public void Device_AllFields()
        {
            var xmlStr = @"
            <assignedAuthor>
                <id root=""4bc8554b-gb7e-4b08-06fe-40c2af85afdb""/>
                <addr>
                    <streetAddressLine nullFlavor=""NI""/>
                    <city nullFlavor=""NI""/>
                    <state nullFlavor=""NI""/>
                    <postalCode nullFlavor=""NI""/>
                    <country nullFlavor=""NI""/>
                </addr>
                <telecom nullFlavor=""NI""/>
                <assignedAuthoringDevice>
                    <manufacturerModelName>""Some_EHR""</manufacturerModelName>
                    <softwareName>2025.9</softwareName>
                </assignedAuthoringDevice>
            </assignedAuthor>
            ";

            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;
            var attributes = new Dictionary<string, object>
            {
                { "author", parsed["assignedAuthor"]},
                { "ID", "1234" },
            };

            var actualFhir = GetFhirObjectFromTemplate<Device>(ECRPath, attributes);

            Assert.Equal(ResourceType.Device.ToString(), actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);
            Assert.NotEmpty(actualFhir.Identifier);
            Assert.Equal("\"Some_EHR\"", actualFhir.Manufacturer);
            Assert.Equal("2025.9", actualFhir.Version.First().Value);
            Assert.Equal("http://hl7.org/fhir/device-category" ,actualFhir.Property.First().Type.Coding.First().System);
            Assert.Equal("software", actualFhir.Property.First().Type.Coding.First().Code);
            Assert.Equal("software", actualFhir.Property.First().Type.Coding.First().Display);
        }
    }
}
