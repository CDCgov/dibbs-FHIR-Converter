using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotLiquid;
using Hl7.Fhir.Model;
using Xunit;
using Microsoft.Health.Fhir.Liquid.Converter.Parsers;
using System;

// using Hl7.Fhir.Model;
// using Hl7.Fhir.Serialization;

namespace Microsoft.Health.Fhir.Liquid.Converter.UnitTests
{
    public class ObservationRRConditionTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "Resource",
            "_ObservationRRCondition.liquid"
        );

        [Fact]
        public void ObservationRRCondition_AllFields()
        {
            // from 3.1 eCR
            var xmlStr = @"
            <observation classCode=""OBS"" moodCode=""EVN""                 
                xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                xsi:schemaLocation=""urn:hl7-org:v3 ../../../cda-core-2.0/schema/extensions/SDTC/infrastructure/cda/CDA_SDTC.xsd""
                xmlns=""urn:hl7-org:v3""
                xmlns:cda=""urn:hl7-org:v3""
                xmlns:sdtc=""urn:hl7-org:sdtc""
                xmlns:voc=""http://www.lantanagroup.com/voc"">
                <templateId extension=""2017-04-01""
                    root=""2.16.840.1.113883.10.20.15.2.3.12"" />
                <id root=""a054d401-7b23-4b15-bc28-c889c156ba6a"" />
                <code code=""64572001"" codeSystem=""2.16.840.1.113883.6.96""
                    codeSystemName=""SNOMED"" displayName=""Condition"">
                    <translation code=""75323-6""
                        codeSystem=""2.16.840.1.113883.6.1""
                        codeSystemName=""LOINC"" displayName=""Condition"" />
                </code>
                <value code=""3928002"" codeSystem=""2.16.840.1.113883.6.96""
                    codeSystemName=""SNOMED CT""
                    displayName=""Zika virus disease (disorder)"" xsi:type=""CD"" />
                <entryRelationship typeCode=""COMP"">
                    <organizer classCode=""CLUSTER"" moodCode=""EVN"">
                        <templateId extension=""2017-04-01""
                            root=""2.16.840.1.113883.10.20.15.2.3.13"" />
                        <id root=""fcf92143-4289-450e-9550-8d574facf626"" />
                        <code code=""RRVS7""
                            codeSystem=""2.16.840.1.114222.4.5.274""
                            codeSystemName=""PHIN VS (CDC Local Coding System)""
                            displayName=""Both patient home address and provider facility address""></code>
                        <statusCode code=""completed"" />
                        <participant typeCode=""LOC"">
                            <templateId extension=""2017-04-01""
                                root=""2.16.840.1.113883.10.20.15.2.4.2"" />
                            <participantRole>
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
                        </participant>
                        <participant typeCode=""LOC"">
                            <templateId extension=""2017-04-01""
                                root=""2.16.840.1.113883.10.20.15.2.4.3"" />
                            <participantRole>
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
                        </participant>
                        <participant typeCode=""LOC"">
                            <templateId extension=""2017-04-01""
                                root=""2.16.840.1.113883.10.20.15.2.4.1"" />
                            <participantRole>
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
                        </participant>
                        <component typeCode=""COMP"">
                            <observation classCode=""OBS"" moodCode=""EVN"">
                                <templateId extension=""2017-04-01""
                                    root=""2.16.840.1.113883.10.20.15.2.3.19"" />
                                <id root=""e39d6ae2-8c6e-4638-9b33-412996586f41"" />
                                <code code=""RR1""
                                    codeSystem=""2.16.840.1.114222.4.5.232""
                                    codeSystemName=""PHIN Questions""
                                    displayName=""Determination of reportability"" />
                                <value code=""RRVS1""
                                    codeSystem=""2.16.840.1.114222.4.5.274""
                                    codeSystemName=""PHIN VS (CDC Local Coding System)""
                                    displayName=""Reportable"" xsi:type=""CD"" />
                                <entryRelationship typeCode=""RSON"">
                                    <observation classCode=""OBS"" moodCode=""EVN"">
                                        <templateId extension=""2017-04-01""
                                            root=""2.16.840.1.113883.10.20.15.2.3.26"" />
                                        <id
                                            root=""8709a342-56ad-425a-b7b1-76a16c2dd2d5"" />
                                        <code code=""RR2""
                                            codeSystem=""2.16.840.1.114222.4.5.232""
                                            codeSystemName=""PHIN Questions""
                                            displayName=""Determination of reportability reason"" />
                                        <value xsi:type=""ST"">Reason for
                                            determination of reportability</value>
                                    </observation>
                                </entryRelationship>
                                <entryRelationship typeCode=""RSON"">
                                    <observation classCode=""OBS"" moodCode=""EVN"">
                                        <templateId extension=""2017-04-01""
                                            root=""2.16.840.1.113883.10.20.15.2.3.27"" />
                                        <id
                                            root=""f2dfdffb-bccb-4ee4-9b6c-0ae82b15ada6"" />
                                        <code code=""RR3""
                                            codeSystem=""2.16.840.1.114222.4.5.232""
                                            codeSystemName=""PHIN Questions""
                                            displayName=""Determination of reportability rule"" />
                                        <value xsi:type=""ST"">Rule used in
                                            reportability determination</value>
                                    </observation>
                                </entryRelationship>
                            </observation>
                        </component>
                    </organizer>
                </entryRelationship>
            </observation>
            ";

            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;
            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "observationEntry", parsed["observation"]},
            };
            var actualFhir = GetFhirObjectFromTemplate<Observation>(ECRPath, attributes);

            Assert.Equal(ResourceType.Observation.ToString(), actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);
            Assert.Equal(ObservationStatus.Final, actualFhir.Status);

            Assert.NotNull(actualFhir.Code);
            Assert.Equal("http://snomed.info/sct", actualFhir.Code?.Coding?.First().System);
            Assert.Equal("Condition", actualFhir.Code?.Coding?.First().Display);
            Assert.Equal("64572001", actualFhir.Code?.Coding?.First().Code);

            Assert.IsType<CodeableConcept>(actualFhir.Value);
            var value = (CodeableConcept)actualFhir.Value;
            Assert.Equal("3928002", value.Coding.First().Code);
            Assert.Equal("http://snomed.info/sct", value.Coding.First().System);
            Assert.Equal("Zika virus disease (disorder)", value.Coding.First().Display);

            Assert.Equal("Reportable", actualFhir.GetExtensionValue<CodeableConcept>("http://hl7.org/fhir/us/ecr/StructureDefinition/us-ph-determination-of-reportability-extension").Coding.First().Display);
            var extRRReason = actualFhir.GetExtension("http://hl7.org/fhir/us/ecr/StructureDefinition/us-ph-determination-of-reportability-reason-extension");
            Assert.Equal("Reason for determination of reportability", ((FhirString)extRRReason.Value).Value);
            var extRRRule = actualFhir.GetExtension("http://hl7.org/fhir/us/ecr/StructureDefinition/us-ph-determination-of-reportability-rule-extension");
            Assert.Equal("Rule used in reportability determination", ((FhirString)extRRRule.Value).Value);
        }
    }
}