using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hl7.Fhir.Model;
using Xunit;
using Dibbs.Fhir.Liquid.Converter.DataParsers;
using System;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests
{
    public class ImmunizationTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "Resource",
            "Immunization.liquid"
        );

        [Fact]
        public void Immunization_AllFields()
        {
            // from 3.1 spec
            var xmlStr = @"
            <substanceAdministration 
                classCode=""SBADM"" 
                moodCode=""EVN"" 
                negationInd=""false""
                xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                xsi:schemaLocation=""urn:hl7-org:v3 ../../../cda-core-2.0/schema/extensions/SDTC/infrastructure/cda/CDA_SDTC.xsd""
                xmlns=""urn:hl7-org:v3""
                xmlns:cda=""urn:hl7-org:v3""
                xmlns:sdtc=""urn:hl7-org:sdtc""
                xmlns:voc=""http://www.lantanagroup.com/voc""
                >
                <!-- ** Immunization activity ** -->
                <templateId root=""2.16.840.1.113883.10.20.22.4.52"" extension=""2015-08-01"" />
                <id root=""e6f1ba43-c0ed-4b9b-9f12-f435d8ad8f92"" />
                <statusCode code=""completed"" />
                <approachSiteCode code=""10013000"" system=""2.16.840.1.113883.6.96"" />
                <effectiveTime value=""19981215"" />
                <routeCode code=""C28161"" codeSystem=""2.16.840.1.113883.3.26.1.1""
                    codeSystemName=""National Cancer Institute (NCI) Thesaurus"" displayName=""Intramuscular
                    injection"" />
                <doseQuantity value=""50"" unit=""ug"" />
                <consumable>
                    <manufacturedProduct classCode=""MANU"">
                        <!-- ** Immunization medication information ** -->
                        <templateId root=""2.16.840.1.113883.10.20.22.4.54"" extension=""2014-06-09"" />
                        <manufacturedMaterial>
                            <code code=""33"" codeSystem=""2.16.840.1.113883.6.59""
                                displayName=""Pneumococcal polysaccharide vaccine"" codeSystemName=""CVX"">
                                <translation code=""854981"" displayName=""Pneumovax 23 (Pneumococcal
                                    vaccine polyvalent) Injectable Solution"" codeSystemName=""RxNORM""
                                    codeSystem=""2.16.840.1.113883.6.88"" />
                            </code>
                            <lotNumberText>1</lotNumberText>
                        </manufacturedMaterial>
                        <manufacturerOrganization>
                            <name>Health LS - Immuno Inc.</name>
                        </manufacturerOrganization>
                    </manufacturedProduct>
                </consumable>
                <entryRelationship typeCode=""COMP"" inversionInd=""true"">
                    <sequenceNumber value=""1"" />
                    <act classCode=""ACT"" moodCode=""EVN"">
                        <!-- Substance Administered Act -->
                        <templateId root=""2.16.840.1.113883.10.20.22.4.118"" />
                        <id root=""00000000-5CF3-EC63-0513-4A4838595787""
                            extension=""11369-6_3-1_1.3.6.1.4.1.22812.11.2016.163.1_14169"" />
                        <code code=""416118004"" codeSystem=""2.16.840.1.113883.6.96"" />
                        <statusCode value=""completed""/>
                        <effectiveTime value=""19981215"" />
                    </act>
                </entryRelationship>
                <entryRelationship typeCode=""RSON"">
                    <observation classCode=""OBS"" moodCode=""EVN"">
                        <templateId root=""2.16.840.1.113883.10.20.22.4.53"" />
                        <id root=""00000000-5CF3-EC63-0513-4A4838595787""
                            extension=""11369-6_3-1_1.3.6.1.4.1.22812.11.2016.163.1_14170"" />
                        <code code=""IMMUNE"" codeSystem=""	2.16.840.1.113883.5.8"" />
                        <statusCode value=""completed""/>
                        <effectiveTime value=""19981215"" />
                    </observation>
                </entryRelationship>
                <performer>
                    <assignedEntity>
                        <id root=""2.16.840.1.113883.19.5.9999.456"" extension=""2981824"" />
                        <addr>
                            <streetAddressLine>1007 Health Drive</streetAddressLine>
                            <city>Portland</city>
                            <state>OR</state>
                            <postalCode>99123</postalCode>
                            <country>US</country>
                        </addr>
                        <telecom use=""WP"" value=""tel: +(555)-555-1030"" />
                        <assignedPerson>
                            <name>
                                <given>Harold</given>
                                <family>Hippocrates</family>
                            </name>
                        </assignedPerson>
                        <representedOrganization>
                            <id root=""2.16.840.1.113883.19.5.9999.1394"" />
                            <name>Good Health Clinic</name>
                            <telecom use=""WP"" value=""tel: +(555)-555-1030"" />
                            <addr>
                                <streetAddressLine>1007 Health Drive</streetAddressLine>
                                <city> Portland </city>
                                <state> OR </state>
                                <postalCode> 99123 </postalCode>
                                <country> US </country>
                            </addr>
                        </representedOrganization>
                    </assignedEntity>
                </performer>
            </substanceAdministration>
            ";
            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "immunization", parsed["substanceAdministration"]},
            };

            var actualFhir = GetFhirObjectFromTemplate<Immunization>(ECRPath, attributes);

            Assert.Equal(ResourceType.Immunization.ToString(), actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);

            Assert.Equal(Immunization.ImmunizationStatusCodes.Completed, actualFhir.Status);

            Assert.Equal("1998-12-15", (actualFhir.Occurrence as FhirDateTime).Value);

            Assert.Equal("IMMUNE", actualFhir.StatusReason.Coding[0].Code);

            Assert.Equal("1", actualFhir.ProtocolApplied[0].DoseNumber.ToString());

            Assert.Equal("33", actualFhir.VaccineCode.Coding[0].Code);
            Assert.Equal("854981", actualFhir.VaccineCode.Coding[1].Code);

            Assert.Equal("1", actualFhir.LotNumber);
            Assert.NotNull(actualFhir.Manufacturer.Reference);

            Assert.Equal("C28161", actualFhir.Route.Coding[0].Code);

            Assert.Equal("10013000", actualFhir.Site.Coding[0].Code);
        }
    }
}
