using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using DotLiquid;
using Hl7.Fhir.Model;
using Microsoft.Health.Fhir.Liquid.Converter.Parsers;
using Xunit;

namespace Microsoft.Health.Fhir.Liquid.Converter.UnitTests
{
    public class EncompassingEncounterFixture
    {
        public class EncounterTests
            : BaseECRLiquidTests,
                IClassFixture<EncompassingEncounterFixture>
        {
            private string eveEverywomanEncompassingEncounterXml =
                @"
<encompassingEncounter
    xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
    xsi:schemaLocation=""urn:hl7-org:v3 ../../../cda-core-2.0/schema/extensions/SDTC/infrastructure/cda/CDA_SDTC.xsd""
    xmlns=""urn:hl7-org:v3""
    xmlns:cda=""urn:hl7-org:v3""
    xmlns:sdtc=""urn:hl7-org:sdtc""
    xmlns:voc=""http://www.lantanagroup.com/voc"">
    <id extension=""9937012"" root=""2.16.840.1.113883.19"" />
    <code code=""AMB"" codeSystem=""2.16.840.1.113883.5.4""
        codeSystemName=""HL7 ActEncounterCode"" displayName=""Ambulatory"" />
    <effectiveTime>
        <low value=""20201107084421-0500"" />
        <high value=""20201108112103-0500"" />
    </effectiveTime>
    <responsibleParty>
        <assignedEntity>
            <id extension=""6666666666666"" root=""2.16.840.1.113883.4.6"" />
            <addr>
                <streetAddressLine>1002 Healthcare Drive</streetAddressLine>
                <city>Ann Arbor</city>
                <state>MI</state>
                <postalCode>99999</postalCode>
                <country>US</country>
            </addr>
            <telecom use=""WP"" value=""tel:+1(555)555-1003"" />
            <telecom use=""WP"" value=""fax:+1(555)555-1234"" />
            <telecom use=""WP"" value=""mailto:mail@provider_domain.com"" />
            <assignedPerson>
                <name>
                    <given>Henry</given>
                    <family>Seven</family>
                    <suffix qualifier=""AC"">M.D.</suffix>
                </name>
            </assignedPerson>
            <representedOrganization>
                <name>Community Health and Hospitals</name>
                <addr>
                    <streetAddressLine>1002 Healthcare Drive</streetAddressLine>
                    <city>Ann Arbor</city>
                    <state>MI</state>
                    <postalCode>99999</postalCode>
                    <country>US</country>
                </addr>
            </representedOrganization>
        </assignedEntity>
    </responsibleParty>
    <location>
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
                <telecom use=""WP"" value=""fax: 1+(555)-555-3333"" />
                <addr>
                    <streetAddressLine>1000 Hospital Lane</streetAddressLine>
                    <city>Ann Arbor</city>
                    <state>MI</state>
                    <postalCode>99999</postalCode>
                    <country>US</country>
                </addr>
            </serviceProviderOrganization>
        </healthCareFacility>
    </location>
</encompassingEncounter>
";

            private string eicr04152020EncompassingEncounterXml =
                @"
<encompassingEncounter
    xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
    xsi:schemaLocation=""urn:hl7-org:v3 ../../../cda-core-2.0/schema/extensions/SDTC/infrastructure/cda/CDA_SDTC.xsd""
    xmlns=""urn:hl7-org:v3""
    xmlns:cda=""urn:hl7-org:v3""
    xmlns:sdtc=""urn:hl7-org:sdtc""
    xmlns:voc=""http://www.lantanagroup.com/voc"">
    <id nullFlavor=""NA""/>
    <code code=""PHC2237"" codeSystem=""2.16.840.1.114222.4.5.274"" codeSystemName=""PHIN VS (CDC Local Coding System)"" displayName=""External Encounter""/>
    <effectiveTime>
        <low nullFlavor=""NA""/>
    </effectiveTime>
    <responsibleParty>
        <assignedEntity>
            <id nullFlavor=""NA"" root=""2.16.840.1.113883.4.6""/>
            <addr>
                <streetAddressLine nullFlavor=""NA""/>
                <city nullFlavor=""NA""/>
                <state nullFlavor=""NA""/>
                <postalCode nullFlavor=""NA""/>
            </addr>
            <telecom nullFlavor=""NA""/>
            <assignedPerson>
                <name>
                    <given nullFlavor=""NA""/>
                    <family nullFlavor=""NA""/>
                </name>
            </assignedPerson>
            <representedOrganization>
                <name nullFlavor=""NA""/>
                <addr>
                    <streetAddressLine nullFlavor=""NA""/>
                    <city nullFlavor=""NA""/>
                    <state nullFlavor=""NA""/>
                    <postalCode nullFlavor=""NA""/>
                </addr>
            </representedOrganization>
        </assignedEntity>
    </responsibleParty>
    <location>
        <healthCareFacility>
            <id nullFlavor=""NA"" root=""2.16.840.1.113883.4.6""/>
            <code nullFlavor=""NA""/>
            <location>
                <addr>
                    <streetAddressLine nullFlavor=""NA""/>
                    <city nullFlavor=""NA""/>
                    <state nullFlavor=""NA""/>
                    <postalCode nullFlavor=""NA""/>
                </addr>
            </location>
            <serviceProviderOrganization>
                <name nullFlavor=""NA""/>
                <telecom nullFlavor=""NA""/>
                <addr>
                    <streetAddressLine nullFlavor=""NA""/>
                    <city nullFlavor=""NA""/>
                    <state nullFlavor=""NA""/>
                    <postalCode nullFlavor=""NA""/>
                </addr>
            </serviceProviderOrganization>
        </healthCareFacility>
    </location>
</encompassingEncounter>";

            private string eCR_RR_combined_3_1EncompassingEncounterXml =
                @"
<encompassingEncounter
    xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
    xsi:schemaLocation=""urn:hl7-org:v3 ../../../cda-core-2.0/schema/extensions/SDTC/infrastructure/cda/CDA_SDTC.xsd""
    xmlns=""urn:hl7-org:v3""
    xmlns:cda=""urn:hl7-org:v3""
    xmlns:sdtc=""urn:hl7-org:sdtc""
    xmlns:voc=""http://www.lantanagroup.com/voc"">
    <id root=""1.2.840.114350.1.13.4304.2.7.3.698084.8"" extension=""XX692"" />
    <code code=""3"" codeSystem=""1.2.840.114350.1.72.1.30"" displayName=""Hospital Encounter"">
        <originalText>Hospital Encounter</originalText>
        <translation code=""0"" codeSystem=""1.2.840.114350.1.72.1.30.1"" />
    </code>
    <effectiveTime>
        <low value=""19811016121446-0500"" />
    </effectiveTime>
    <responsibleParty>
        <assignedEntity>
            <id root=""2.16.840.1.113883.4.6"" extension=""XX13576542"" />
            <addr use=""WP"">
                <streetAddressLine>54018 KAMINO OCEAN Spire</streetAddressLine>
                <streetAddressLine>STE 820</streetAddressLine>
                <city>Keren</city>
                <state>YA</state>
                <postalCode>81303</postalCode>
            </addr>
            <telecom use=""WP"" value=""tel:+6-901-374-4286"" />
            <assignedPerson>
                <name>
                    <given>Elzar</given>
                    <family>Baba</family>
                </name>
            </assignedPerson>
            <representedOrganization>
                <name>Academy of Agamar Research Institute</name>
                <addr use=""WP"">
                    <streetAddressLine>554 KAMINO OCEAN Street</streetAddressLine>
                    <streetAddressLine>Building SubaddressIdentifier</streetAddressLine>
                    <county>Lah'mu</county>
                    <city>Galactic City</city>
                    <state>KZ</state>
                    <postalCode>79572</postalCode>
                    <country>AI</country>
                </addr>
            </representedOrganization>
        </assignedEntity>
    </responsibleParty>
    <encounterParticipant typeCode=""ATND"">
        <time value=""19811016121446-0500"" />
        <assignedEntity>
            <id root=""2.16.840.1.113883.4.6"" extension=""1234561235"" />
            <code code=""208D00000X"" codeSystem=""2.16.840.1.113883.6.101""
                displayName=""General Practice Physician"">
                <originalText>Internal Medicine</originalText>
                <translation code=""32"" codeSystem=""1.2.840.114350.1.72.1.7.7.10.688867.4160""
                    codeSystemName=""Epic.DXC.StandardProviderSpecialtyType""
                    displayName=""Internal Medicine"" />
                <translation code=""17""
                    codeSystem=""1.2.840.114350.1.13.4304.2.7.10.836982.1050""
                    codeSystemName=""Epic.SER.ProviderSpecialty""
                    displayName=""Internal Medicine"" />
            </code>
            <addr use=""WP"">
                <streetAddressLine>90001 TEST Avey</streetAddressLine>
                <streetAddressLine>STE 100</streetAddressLine>
                <city>Washington</city>
                <state>DC</state>
                <postalCode>20000</postalCode>
            </addr>
            <telecom use=""WP"" value=""tel:+1-608-271-9000"" />
            <assignedPerson>
                <name use=""L"">
                    <given>JohnTESTPROVIDER</given>
                    <family>Powers</family>
                    <suffix qualifier=""AC""> MD</suffix>
                </name>
            </assignedPerson>
        </assignedEntity>
    </encounterParticipant>
    <encounterParticipant typeCode=""ADM"">
        <time value=""19811016121446-0500"" />
        <assignedEntity>
            <id root=""2.16.840.1.113883.4.6"" extension=""1234561235"" />
            <code code=""208D00000X"" codeSystem=""2.16.840.1.113883.6.101""
                displayName=""General Practice Physician"">
                <originalText>Internal Medicine</originalText>
                <translation code=""32"" codeSystem=""1.2.840.114350.1.72.1.7.7.10.688867.4160""
                    codeSystemName=""Epic.DXC.StandardProviderSpecialtyType""
                    displayName=""Internal Medicine"" />
                <translation code=""17""
                    codeSystem=""1.2.840.114350.1.13.4304.2.7.10.836982.1050""
                    codeSystemName=""Epic.SER.ProviderSpecialty""
                    displayName=""Internal Medicine"" />
            </code>
            <addr use=""WP"">
                <streetAddressLine>90001 TEST Avey</streetAddressLine>
                <streetAddressLine>STE 100</streetAddressLine>
                <city>Washington</city>
                <state>DC</state>
                <postalCode>20000</postalCode>
            </addr>
            <telecom use=""WP"" value=""tel:+1-608-271-9000"" />
            <assignedPerson>
                <name use=""L"">
                    <given>JohnTESTPROVIDER</given>
                    <family>Powers</family>
                    <suffix qualifier=""AC""> MD</suffix>
                </name>
            </assignedPerson>
        </assignedEntity>
    </encounterParticipant>
    <location>
        <healthCareFacility>
            <id root=""1.2.840.114350.1.13.4304.2.7.2.686980"" extension=""XX114730"" />
            <code code=""1081-9"" codeSystem=""2.16.840.1.113883.6.259""
                displayName=""Pediatric Medical-Surgical Ward"">
                <originalText>Pediatrics</originalText>
                <translation code=""82"" codeSystem=""1.2.840.114350.1.72.1.7.7.10.688867.4150""
                    codeSystemName=""Epic.DepartmentSpecialty"" displayName=""Pediatrics"" />
            </code>
            <location>
                <name> Sissubo School Neighbourhood Laboratory</name>
                <addr use=""WP"">
                    <streetAddressLine>4509 KAMINO OCEAN Street</streetAddressLine>
                    <county>Lah'mu</county>
                    <city>Galactic City</city>
                    <state>KZ</state>
                    <postalCode>89170-2785</postalCode>
                    <country>AI</country>
                </addr>
            </location>
            <serviceProviderOrganization>
                <id root=""1.2.840.114350.1.13.4304.2.7.2.696570"" extension=""XX7530"" />
                <name>Institute of Takodana Neighbourhood Pharmacy &amp; Hospital</name>
                <telecom use=""WP"" value=""tel:+3-298-900-4460-y60"" />
                <addr use=""WP"">
                    <streetAddressLine>554 KAMINO OCEAN Street</streetAddressLine>
                    <county>Lah'mu</county>
                    <city>Galactic City</city>
                    <state>KZ</state>
                    <postalCode>89170-2785</postalCode>
                    <country>AI</country>
                </addr>
                <asOrganizationPartOf>
                    <wholeOrganization>
                        <name>Academy of Agamar Research Institute</name>
                        <addr use=""WP"">
                            <streetAddressLine>554 KAMINO OCEAN Street</streetAddressLine>
                            <streetAddressLine>Building SubaddressIdentifier</streetAddressLine>
                            <county>Lah'mu</county>
                            <city>Galactic City</city>
                            <state>KZ</state>
                            <postalCode>79572</postalCode>
                            <country>AI</country>
                        </addr>
                    </wholeOrganization>
                </asOrganizationPartOf>
            </serviceProviderOrganization>
        </healthCareFacility>
    </location>
</encompassingEncounter>";

            private static readonly string ECRPath = Path.Join(
                TestConstants.ECRTemplateDirectory,
                "Resource",
                "_Encounter.liquid"
            );

            public Encounter ConvertEncompassingEncounter(string encompassingEncounterXml)
            {
                var parsed =
                    new CcdaDataParser().Parse(encompassingEncounterXml)
                    as Dictionary<string, object>;

                var attributes = new Dictionary<string, object>
                {
                    { "ID", "1234" },
                    { "encounter", parsed["encompassingEncounter"] },
                };

                return GetFhirObjectFromTemplate<Encounter>(ECRPath, attributes);
            }

            [Fact]
            public void IdBecomesIdentifier()
            {
                var actualFhir = ConvertEncompassingEncounter(
                    eveEverywomanEncompassingEncounterXml
                );
                Assert.Equal("urn:oid:2.16.840.1.113883.19", actualFhir.Identifier.First().System);
                Assert.Equal("9937012", actualFhir.Identifier.First().Value);
            }

            [Fact]
            public void CodeBecomesClass()
            {
                var actualFhir = ConvertEncompassingEncounter(
                    eveEverywomanEncompassingEncounterXml
                );
                Assert.Equal("AMB", actualFhir.Class.Code);
                Assert.Equal(
                    "urn:oid:2.16.840.1.113883.5.4",
                    actualFhir.Class.System
                );
                Assert.Equal("Ambulatory", actualFhir.Class.Display);
            }

            [Fact]
            public void CodeBecomesClass_ExternalEncounter()
            {
                var actualFhir = ConvertEncompassingEncounter(eicr04152020EncompassingEncounterXml);
                Assert.Equal("PHC2237", actualFhir.Class.Code);
                Assert.Equal(
                    "urn:oid:2.16.840.1.114222.4.5.274",
                    actualFhir.Class.System
                );
                Assert.Equal("External Encounter", actualFhir.Class.Display);
            }

            // [Fact]
            // public void CodeBecomesClass_Unknown()
            // {
            //     var actualFhir = ConvertEncompassingEncounter(eCR_RR_combined_3_1EncompassingEncounterXml);
            //     Assert.Equal("unknown", actualFhir.Class.Code);
            //     Assert.Equal(
            //         "http://terminology.hl7.org/CodeSystem/data-absent-reason",
            //         actualFhir.Class.System
            //     );
            //     Assert.Equal("Unknown", actualFhir.Class.Display);
            // }

            [Fact]
            public void Status_SingleTimestamp()
            {
                var xmlStr =
                    @"
<encompassingEncounter
    xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
    xsi:schemaLocation=""urn:hl7-org:v3 ../../../cda-core-2.0/schema/extensions/SDTC/infrastructure/cda/CDA_SDTC.xsd""
    xmlns=""urn:hl7-org:v3""
    xmlns:cda=""urn:hl7-org:v3""
    xmlns:sdtc=""urn:hl7-org:sdtc""
    xmlns:voc=""http://www.lantanagroup.com/voc"">
    <id nullFlavor=""NA""/>
    <code code=""PHC2237"" codeSystem=""2.16.840.1.114222.4.5.274"" codeSystemName=""PHIN VS (CDC Local Coding System)"" displayName=""External Encounter""/>
    <effectiveTime value= ""20000101""/>
</encompassingEncounter>";

                var actualFhir = ConvertEncompassingEncounter(xmlStr);
                Assert.Equal(Encounter.EncounterStatus.Finished, actualFhir.Status);
            }

            [Fact]
            public void Status_HighLow()
            {
                var xmlStr =
                    @"
<encompassingEncounter
    xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
    xsi:schemaLocation=""urn:hl7-org:v3 ../../../cda-core-2.0/schema/extensions/SDTC/infrastructure/cda/CDA_SDTC.xsd""
    xmlns=""urn:hl7-org:v3""
    xmlns:cda=""urn:hl7-org:v3""
    xmlns:sdtc=""urn:hl7-org:sdtc""
    xmlns:voc=""http://www.lantanagroup.com/voc"">
    <id nullFlavor=""NA""/>
    <code code=""PHC2237"" codeSystem=""2.16.840.1.114222.4.5.274"" codeSystemName=""PHIN VS (CDC Local Coding System)"" displayName=""External Encounter""/>
    <effectiveTime>
        <low value=""20201107084421-0500""/>
        <high value=""20201108112103-0500""/>
    </effectiveTime>
</encompassingEncounter>";

                var actualFhir = ConvertEncompassingEncounter(xmlStr);
                Assert.Equal(Encounter.EncounterStatus.Finished, actualFhir.Status);
            }

            [Fact]
            public void Status_Low()
            {
                var xmlStr =
                    @"
<encompassingEncounter
    xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
    xsi:schemaLocation=""urn:hl7-org:v3 ../../../cda-core-2.0/schema/extensions/SDTC/infrastructure/cda/CDA_SDTC.xsd""
    xmlns=""urn:hl7-org:v3""
    xmlns:cda=""urn:hl7-org:v3""
    xmlns:sdtc=""urn:hl7-org:sdtc""
    xmlns:voc=""http://www.lantanagroup.com/voc"">
    <id nullFlavor=""NA""/>
    <code code=""PHC2237"" codeSystem=""2.16.840.1.114222.4.5.274"" codeSystemName=""PHIN VS (CDC Local Coding System)"" displayName=""External Encounter""/>
    <effectiveTime>
        <low value=""20000101""/>
    </effectiveTime>
</encompassingEncounter>";

                var actualFhir = ConvertEncompassingEncounter(xmlStr);
                Assert.Equal(Encounter.EncounterStatus.InProgress, actualFhir.Status);
            }
        }
    }
}
