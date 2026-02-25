using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Hl7.Fhir.Model;
using Dibbs.Fhir.Liquid.Converter.DataParsers;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests
{
    public class LocationTests : BaseECRLiquidTests
    {
        private static readonly string ECRPathLocation = Path.Join(
            TestConstants.ECRTemplateDirectory, "Resource", "Location.liquid"
        );

        private static readonly string ECRPathLocationHealthCareFacility = Path.Join(
            TestConstants.ECRTemplateDirectory, "Resource", "LocationHealthCareFacility.liquid"
        );

        [Fact]
        public void Location_Basic_AllFields()
        {
            // from 3.1 Eve Everywoman
            // Added the ID field so we have all required fields
            var xmlStr = @"
                <participantRole classCode=""SDLOC"">
                    <templateId root=""2.16.840.1.113883.10.20.22.4.32"" />
                    <code code=""1160-1"" codeSystem=""2.16.840.1.113883.6.259""
                        codeSystemName=""HealthcareServiceLocation""
                        displayName=""Community Health and Hospitals"" />
                    <id extension=""77777777777"" root=""2.16.840.1.113883.4.6"" />
                    <addr>
                        <streetAddressLine>1002 Healthcare Drive</streetAddressLine>
                        <city>Ann Arbor</city>
                        <state>MI</state>
                        <postalCode>99999</postalCode>
                        <country>US</country>
                    </addr>
                    <telecom use=""WP"" value=""tel:+1(555)555-5000"" />
                    <playingEntity classCode=""PLC"">
                        <name>Community Health and Hospitals</name>
                    </playingEntity>
                </participantRole>
            ";
            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "location", parsed["participantRole"]},
            };

            var actualFhir = GetFhirObjectFromTemplate<Location>(ECRPathLocation, attributes);

            Assert.Equal(ResourceType.Location.ToString(), actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);
            Assert.Equal("http://hl7.org/fhir/us/ecr/StructureDefinition/us-ph-location", actualFhir.Meta.Profile.First());

            // Required fields: identifier, name, address, type
            Assert.Equal("http://hl7.org/fhir/sid/us-npi", actualFhir.Identifier.First().System);
            Assert.Equal("77777777777", actualFhir.Identifier.First().Value);

            Assert.Equal("Community Health and Hospitals", actualFhir.Name);

            Assert.Equal("Ann Arbor", actualFhir.Address.City);
            Assert.Equal("MI", actualFhir.Address.State);
            Assert.Equal("1002 Healthcare Drive", actualFhir.Address.Line.First());
            Assert.Equal("US", actualFhir.Address.Country);
            Assert.Equal("99999", actualFhir.Address.PostalCode);

            Assert.Equal("1160-1", actualFhir.Type.First().Coding.First().Code);
            Assert.Equal("urn:oid:2.16.840.1.113883.6.259", actualFhir.Type.First().Coding.First().System);
            Assert.Equal("Community Health and Hospitals", actualFhir.Type.First().Coding.First().Display);

            // Not required fields: telecom
            Assert.Equal("Phone", actualFhir.Telecom.First().System.ToString());
            Assert.Equal("+1(555)555-5000", actualFhir.Telecom.First().Value);
            Assert.Equal("Work", actualFhir.Telecom.First().Use.ToString());
        }

        [Fact]
        public void Location_HealthCareFacility_Basic_AllFields()
        {
            // from 3.1 Eve Everywoman
            var xmlStr = @"
                <healthCareFacility>
                    <id extension=""77777777777"" root=""2.16.840.1.113883.4.6"" />
                    <code code=""OF"" codeSystem=""2.16.840.1.113883.5.111""
                        displayName=""Outpatient facility"" />
                    <location>
                        <addr>
                            <streetAddressLine>1000 Hospital Lane</streetAddressLine>
                            <city>Ann Arbor</city>
                            <state>MI</state>
                            <postalCode>99999</postalCode>
                            <country>US</country>
                        </addr>
                    </location>
                    <serviceProviderOrganization>
                        <name>Good Health Hospital</name>
                        <telecom use=""WP"" value=""tel: 1+(555)-555-1212"" />
                    </serviceProviderOrganization>
                </healthCareFacility>
            ";
            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "location", parsed["healthCareFacility"]},
            };

            var actualFhir = GetFhirObjectFromTemplate<Location>(ECRPathLocationHealthCareFacility, attributes);

            Assert.Equal(ResourceType.Location.ToString(), actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);
            Assert.Equal("http://hl7.org/fhir/us/ecr/StructureDefinition/us-ph-location", actualFhir.Meta.Profile.First());

            // Required fields: identifier, name, address, type
            Assert.Equal("http://hl7.org/fhir/sid/us-npi", actualFhir.Identifier.First().System);
            Assert.Equal("77777777777", actualFhir.Identifier.First().Value);

            Assert.Equal("Good Health Hospital", actualFhir.Name);

            Assert.Equal("Ann Arbor", actualFhir.Address.City);
            Assert.Equal("MI", actualFhir.Address.State);
            Assert.Equal("1000 Hospital Lane", actualFhir.Address.Line.First());
            Assert.Equal("US", actualFhir.Address.Country);
            Assert.Equal("99999", actualFhir.Address.PostalCode);

            Assert.Equal("OF", actualFhir.Type.First().Coding.First().Code);
            Assert.Equal("urn:oid:2.16.840.1.113883.5.111", actualFhir.Type.First().Coding.First().System);
            Assert.Equal("Outpatient facility", actualFhir.Type.First().Coding.First().Display);

            // Not required fields: telecom
            Assert.Equal("Phone", actualFhir.Telecom.First().System.ToString());
            Assert.Equal("1+(555)-555-1212", actualFhir.Telecom.First().Value);
            Assert.Equal("Work", actualFhir.Telecom.First().Use.ToString());
        }

        [Fact]
        public void Location_Basic_RequiredFieldsAreAbsent()
        {
            // from 3.1 spec
            var xmlStr = @"
                <participantRole>
                <id nullFlavor=""NA"" root=""2.16.840.1.113883.4.6""/>
                    <code nullFlavor=""NA""/>
                    <playingEntity classCode=""PLC"">
                        <name nullFlavor=""NA"" />
                    </playingEntity>
                    <addr>
                        <streetAddressLine nullFlavor=""NA""/>
                        <city/>
                        <state nullFlavor=""NASK""/>
                        <postalCode nullFlavor=""NA""/>
                    </addr>
                </participantRole>
            ";
            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "location", parsed["participantRole"]},
            };

            var actualFhir = GetFhirObjectFromTemplate<Location>(ECRPathLocation, attributes);

            Assert.Equal(ResourceType.Location.ToString(), actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);
            Assert.Equal("http://hl7.org/fhir/us/ecr/StructureDefinition/us-ph-location", actualFhir.Meta.Profile.First());

            // Required fields: identifier, name, address, type should be null
            // Still valid bc they have data absent extensions/codeable concepts
            Assert.Equal("http://hl7.org/fhir/sid/us-npi", actualFhir.Identifier.First().System);
            Assert.Null(actualFhir.Identifier.First().Value);
            Assert.Null(actualFhir.Name);

            Assert.NotNull(actualFhir.Address);
            Assert.NotEmpty(actualFhir.Address.CityElement.Extension);
            var AddressCity = actualFhir.Address.CityElement.Extension.FirstOrDefault(e => e.Url == "http://hl7.org/fhir/StructureDefinition/data-absent-reason");
            var AddressCityDataAbsentReason = (AddressCity.Value as Code)?.Value;
            Assert.Equal("unknown", AddressCityDataAbsentReason);

            Assert.NotEmpty(actualFhir.Address.StateElement.Extension);
            var AddressState = actualFhir.Address.StateElement.Extension.FirstOrDefault(e => e.Url == "http://hl7.org/fhir/StructureDefinition/data-absent-reason");
            var AddressStateDataAbsentReason = (AddressState.Value as Code)?.Value;
            Assert.Equal("not-asked", AddressStateDataAbsentReason);

            Assert.Equal("not-applicable", actualFhir.Type.First().Coding.First().Code);
            Assert.Equal("http://terminology.hl7.org/CodeSystem/data-absent-reason", actualFhir.Type.First().Coding.First().System);
        }

    }
}
