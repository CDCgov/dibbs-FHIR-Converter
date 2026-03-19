using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hl7.Fhir.Model;
using Xunit;
using Dibbs.Fhir.Liquid.Converter.DataParsers;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests
{
    public class CareTeamTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "Resource",
            "CareTeam.liquid"
        );

        [Fact]
        public void CareTeam_AllFields()
        {
            var xmlStr = @"
              <!--Care Team Organizer-->
              <organizer classCode=""CLUSTER"" moodCode=""EVN"">
                <templateId root=""2.16.840.1.113883.10.20.22.4.500"" extension=""2019-07-01""/>
                <templateId root=""2.16.840.1.113883.10.20.22.4.500"" extension=""2022-06-01""/>
                <id root=""c37b6e41-8d99-496f-afba-b97383da63eb""/>
                <code code=""86744-0"" codeSystem=""2.16.840.1.113883.6.1"" codeSystemName=""LOINC"" displayName=""Care Team"">
                  <originalText>
                    <reference value=""#CareTeamName1""/>
                  </originalText>
                </code>
                <!--Care Team Status - https://vsac.nlm.nih.gov/valueset/2.16.840.1.113883.1.11.15933/expansion-->
                <statusCode code=""active""/>
                <effectiveTime>
                  <low value=""201810081426-0500""/>
                </effectiveTime>
                <!-- This participant is the Care Team Lead (1..1)-->
                <!-- Care Team Lead is one of the contained care team members in the list of care team members-->
                <participant typeCode=""PPRF"">
                  <participantRole>
                    <!--<This id matches at least one of the member's id in the Care Team Member act template-->
                    <id root=""1.5.5.5.5.5.5""/>
                  </participantRole>
                </participant>
                <!-- #1 Care Team Member Act - This component is a care team member who is a provider -->
                <component>
                  <act classCode=""PCPR"" moodCode=""EVN"">
                    <templateId root=""2.16.840.1.113883.10.20.22.4.500.1"" extension=""2019-07-01""/>
                    <templateId root=""2.16.840.1.113883.10.20.22.4.500.1"" extension=""2022-06-01""/>
                    <id root=""1.5.5.5.5.5.5""/>
                    <code code=""85847-2"" codeSystem=""2.16.840.1.113883.6.1"" codeSystemName=""LOINC"" displayName=""Care Team Information""/>
                    <text>
                      <reference value=""#CareTeamParticipants1"" />
                    </text>
                    <!--Care Team Member Status - https://vsac.nlm.nih.gov/valueset/2.16.840.1.113883.1.11.15933/expansion-->
                    <statusCode code=""active""/>
                    <effectiveTime xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""IVL_TS"">
                      <low value=""201810081426-0500""/>
                    </effectiveTime>
                    <!--Attributes about the provider member - name-->
                    <performer typeCode=""PRF"">
                      <functionCode xmlns=""urn:hl7-org:sdtc"" code=""PCP"" displayName=""primary care physician"" codeSystem=""2.16.840.1.113883.5.88"" codeSystemName=""ParticipationFunction"" />
                      <assignedEntity>
                        <id root=""B00B14E8-CDE4-48EA-8A09-01BC4945122A"" extension=""1""/>
                        <id root=""1.5.5.5.5.5.5""/>
                        <!-- This is a fictional NPI so it will not validate if checked -->
                        <id extension=""5555555555"" root=""2.16.840.1.113883.4.6""/>
                        <addr>
                          <streetAddressLine>100 Main St. Suite 100</streetAddressLine>
                          <city>Hope Valley</city>
                          <state>RI</state>
                          <postalCode>02832</postalCode>
                          <country>US</country>
                        </addr>
                        <telecom use=""WP"" value=""tel:+1(401)539-2461""/>
                        <telecom value=""mailto:johndsmith@direct.aclinic.org""/>
                        <assignedPerson>
                          <name>
                            <given>John</given>
                            <given>D</given>
                            <family>Smith</family>
                            <suffix>MD</suffix>
                          </name>
                        </assignedPerson>
                        <representedOrganization>
                          <id extension=""219BX"" root=""1.2.16.840.1.113883.4.6""/>
                          <name>Hope Woods Health Services</name>
                        </representedOrganization>
                      </assignedEntity>
                    </performer>
                  </act>
                </component>
              </organizer>";

            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "patientId", "Patient/3456" },
                { "careTeamEntry", parsed["organizer"]},
            };

            var actualFhir = GetFhirObjectFromTemplate<CareTeam>(ECRPath, attributes);

            Assert.Equal("CareTeam", actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);
            Assert.NotEmpty(actualFhir.Identifier);
            Assert.Equal(CareTeam.CareTeamStatus.Active, actualFhir.Status);
            Assert.Equal("Patient/3456", actualFhir.Subject.Reference);
            Assert.Equal("201810081426-0500", actualFhir.Period.Start);
            Assert.Empty(actualFhir.Period.End);
            
            Assert.NotEmpty(actualFhir.Participant.First().Role);
            Assert.IsType<CodeableConcept>(actualFhir.Participant.First().Role.First());
            var roleCodeableConcept = (CodeableConcept)actualFhir.Participant.First().Role.First();
            
            Assert.Equal("primary care physician", roleCodeableConcept.Coding.First().Display);
            Assert.Equal("urn:oid:2.16.840.1.113883.5.88", roleCodeableConcept.Coding.First().System);
            Assert.Equal("PCP", roleCodeableConcept.Coding.First().Code);
            Assert.NotNull(actualFhir.Participant[0].Member.Reference);
            Assert.Equal("201810081426-0500", actualFhir.Participant[0].Period.Start);
            Assert.Empty(actualFhir.Participant[0].Period.End);
            Assert.Equal("participant.status", actualFhir.Participant[0].ModifierExtension[0].Url);
            Assert.Equal("active", actualFhir.Participant[0].ModifierExtension[0].Value.ToString());
        }
    }
}
