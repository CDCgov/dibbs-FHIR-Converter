using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotLiquid;
using Hl7.Fhir.Model;
using Xunit;
using Microsoft.Health.Fhir.Liquid.Converter.Parsers;
using System;

namespace Microsoft.Health.Fhir.Liquid.Converter.UnitTests
{
    public class OrganizationRRTests : BaseECRLiquidTests
    {
        private static readonly string ECRPathRoutingEntity = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "Resource",
            "_OrganizationRRRoutingEntity.liquid"
        );

        private static readonly string ECRPathResponsibleAgency = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "Resource",
            "_OrganizationRRResponsibleAgency.liquid"
        );

        private static readonly string ECRPathRulesAuthoringAgency = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "Resource",
            "_OrganizationRRRulesAuthoringAgency.liquid"
        );

        [Fact]
        public void OrganizationRRRoutingEntity_AllFields()
        {
            // from 3.1 eCR
            var xmlStr = @"
            <participantRole
                xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                xsi:schemaLocation=""urn:hl7-org:v3 ../../../cda-core-2.0/schema/extensions/SDTC/infrastructure/cda/CDA_SDTC.xsd""
                xmlns=""urn:hl7-org:v3""
                xmlns:cda=""urn:hl7-org:v3""
                xmlns:sdtc=""urn:hl7-org:sdtc""
                xmlns:voc=""http://www.lantanagroup.com/voc"">
                <id extension=""43214321""
                    root=""2.16.840.1.113883.4.6"" />
                <code code=""RR7""
                    codeSystem=""2.16.840.1.114222.4.5.232""
                    codeSystemName=""PHIN Questions""
                    displayName=""Routing Entity"" />
                <addr use=""WP"">
                    <streetAddressLine>7777 Health Authority
                        Drive</streetAddressLine>
                    <city>City</city>
                    <state>State</state>
                    <postalCode>99999</postalCode>
                </addr>
                <telecom use=""WP"" value=""tel:+1-555-555-3555"" />
                <telecom use=""WP"" value=""fax:+1-955-555-3555"" />
                <telecom use=""WP""
                    value=""mailto:mail@healthauthoritywest.gov"" />
                <telecom use=""WP""
                    value=""https://www.healthauthoritywest.gov"" />
                <playingEntity>
                    <name>State Department of Health Routing
                        Agency</name>
                    <desc>Routing Agency Description</desc>
                </playingEntity>
            </participantRole>
            ";

            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;
            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "organization", parsed["participantRole"]},
            };
            var actualFhir = GetFhirObjectFromTemplate<Organization>(ECRPathRoutingEntity, attributes);

            Assert.Equal(ResourceType.Organization.ToString(), actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);
            Assert.Equal(
                "http://hl7.org/fhir/us/ecr/StructureDefinition/rr-routing-entity-organization",
                actualFhir.Meta.Profile.First()
            );
            Assert.Equal(true, actualFhir.Active);

            Assert.Equal("Routing Entity", actualFhir.Type.First().Coding.First().Display);
            Assert.Equal("RR7", actualFhir.Type.First().Coding.First().Code);
            Assert.Equal("State Department of Health Routing Agency", actualFhir.Name);

            Assert.Equal("+1-555-555-3555", actualFhir.Telecom.Single(t => t.System == ContactPoint.ContactPointSystem.Phone).Value);
            Assert.Equal("+1-955-555-3555", actualFhir.Telecom.Single(t => t.System == ContactPoint.ContactPointSystem.Fax).Value);
            Assert.Equal("mail@healthauthoritywest.gov", actualFhir.Telecom.Single(t => t.System == ContactPoint.ContactPointSystem.Email).Value);
            Assert.Equal("https://www.healthauthoritywest.gov", actualFhir.Telecom.Single(t => t.System == null).Value);

            Assert.Equal("7777 Health Authority Drive", actualFhir.Address.First().Line.First());
            Assert.Equal("City", actualFhir.Address.First().City);
            Assert.Equal("State", actualFhir.Address.First().State);
            Assert.Equal("99999", actualFhir.Address.First().PostalCode);
            Assert.Equal(Address.AddressUse.Work, actualFhir.Address.First().Use);
        }

        [Fact]
        public void OrganizationRResponsibleAgency_AllFields()
        {
            // from 3.1 eCR
            var xmlStr = @"
            <participantRole
                xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                xsi:schemaLocation=""urn:hl7-org:v3 ../../../cda-core-2.0/schema/extensions/SDTC/infrastructure/cda/CDA_SDTC.xsd""
                xmlns=""urn:hl7-org:v3""
                xmlns:cda=""urn:hl7-org:v3""
                xmlns:sdtc=""urn:hl7-org:sdtc""
                xmlns:voc=""http://www.lantanagroup.com/voc"">
                <id extension=""12341234""
                    root=""2.16.840.1.113883.4.6"" />
                <code code=""RR8""
                    codeSystem=""2.16.840.1.114222.4.5.232""
                    codeSystemName=""PHIN Questions""
                    displayName=""Responsible Agency""></code>
                <addr use=""WP"">
                    <streetAddressLine>7777 Health Authority
                        Drive</streetAddressLine>
                    <city />
                    <state>State</state>
                    <postalCode>99999</postalCode>
                </addr>
                <telecom use=""WP"" value=""tel:+1-555-555-3555"" />
                <telecom use=""WP"" value=""fax:+1-955-555-3555"" />
                <telecom use=""WP""
                    value=""mailto:mail@healthauthoritywest.gov"" />
                <telecom use=""WP""
                    value=""https://www.healthauthoritywest.gov"" />
                <playingEntity>
                    <name>State Department of Health</name>
                    <desc>Responsible Agency Description</desc>
                </playingEntity>
            </participantRole>
            ";

            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;
            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "organization", parsed["participantRole"]},
            };
            var actualFhir = GetFhirObjectFromTemplate<Organization>(ECRPathResponsibleAgency, attributes);

            Assert.Equal(ResourceType.Organization.ToString(), actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);
            Assert.Equal(
                "http://hl7.org/fhir/us/ecr/StructureDefinition/rr-responsible-agency-organization",
                actualFhir.Meta.Profile.First()
            );
            Assert.Equal(true, actualFhir.Active);

            Assert.Equal("Responsible Agency", actualFhir.Type.First().Coding.First().Display);
            Assert.Equal("RR8", actualFhir.Type.First().Coding.First().Code);
            Assert.Equal("State Department of Health", actualFhir.Name);

            Assert.Equal("+1-555-555-3555", actualFhir.Telecom.Single(t => t.System == ContactPoint.ContactPointSystem.Phone).Value);
            Assert.Equal("+1-955-555-3555", actualFhir.Telecom.Single(t => t.System == ContactPoint.ContactPointSystem.Fax).Value);
            Assert.Equal("mail@healthauthoritywest.gov", actualFhir.Telecom.Single(t => t.System == ContactPoint.ContactPointSystem.Email).Value);
            Assert.Equal("https://www.healthauthoritywest.gov", actualFhir.Telecom.Single(t => t.System == null).Value);

            Assert.Equal("7777 Health Authority Drive", actualFhir.Address.First().Line.First());
            Assert.Equal("State", actualFhir.Address.First().State);
            Assert.Equal("99999", actualFhir.Address.First().PostalCode);
            Assert.Equal(Address.AddressUse.Work, actualFhir.Address.First().Use);
        }
        
        [Fact]
        public void OrganizationRRRulesAuthoringAgency_AllFields()
        {
            // from 3.1 eCR
            var xmlStr = @"
            <participantRole
                xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                xsi:schemaLocation=""urn:hl7-org:v3 ../../../cda-core-2.0/schema/extensions/SDTC/infrastructure/cda/CDA_SDTC.xsd""
                xmlns=""urn:hl7-org:v3""
                xmlns:cda=""urn:hl7-org:v3""
                xmlns:sdtc=""urn:hl7-org:sdtc""
                xmlns:voc=""http://www.lantanagroup.com/voc"">
                <id extension=""12341234""
                    root=""2.16.840.1.113883.4.6"" />
                <code code=""RR12""
                    codeSystem=""2.16.840.1.114222.4.5.232""
                    codeSystemName=""PHIN Questions""
                    displayName=""Rules Authoring Agency"" />
                <addr use=""WP"">
                    <streetAddressLine>7777 Health Authority
                        Drive</streetAddressLine>
                    <city>City</city>
                    <state>State</state>
                    <postalCode>99999</postalCode>
                </addr>
                <telecom use=""WP"" value=""tel:+1-555-555-3555"" />
                <telecom use=""WP"" value=""fax:+1-955-555-3555"" />
                <telecom use=""WP""
                    value=""mailto:mail@healthauthoritywest.gov"" />
                <telecom use=""WP""
                    value=""https://www.healthauthoritywest.gov"" />
                <playingEntity>
                    <name>State Department of Health</name>
                    <desc>Rules Authoring Agency Description</desc>
                </playingEntity>
            </participantRole>
            ";

            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;
            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "organization", parsed["participantRole"]},
            };
            var actualFhir = GetFhirObjectFromTemplate<Organization>(ECRPathRulesAuthoringAgency, attributes);

            Assert.Equal(ResourceType.Organization.ToString(), actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);
            Assert.Equal(
                "http://hl7.org/fhir/us/ecr/StructureDefinition/rr-rules-authoring-agency-organization",
                actualFhir.Meta.Profile.First()
            );
            Assert.Equal(true, actualFhir.Active);

            Assert.Equal("Rules Authoring Agency", actualFhir.Type.First().Coding.First().Display);
            Assert.Equal("RR12", actualFhir.Type.First().Coding.First().Code);
            Assert.Equal("State Department of Health", actualFhir.Name);

            Assert.Equal("+1-555-555-3555", actualFhir.Telecom.Single(t => t.System == ContactPoint.ContactPointSystem.Phone).Value);
            Assert.Equal("+1-955-555-3555", actualFhir.Telecom.Single(t => t.System == ContactPoint.ContactPointSystem.Fax).Value);
            Assert.Equal("mail@healthauthoritywest.gov", actualFhir.Telecom.Single(t => t.System == ContactPoint.ContactPointSystem.Email).Value);
            Assert.Equal("https://www.healthauthoritywest.gov", actualFhir.Telecom.Single(t => t.System == null).Value);

            Assert.Equal("7777 Health Authority Drive", actualFhir.Address.First().Line.First());
            Assert.Equal("City", actualFhir.Address.First().City);
            Assert.Equal("State", actualFhir.Address.First().State);
            Assert.Equal("99999", actualFhir.Address.First().PostalCode);
            Assert.Equal(Address.AddressUse.Work, actualFhir.Address.First().Use);
        }
    }
}