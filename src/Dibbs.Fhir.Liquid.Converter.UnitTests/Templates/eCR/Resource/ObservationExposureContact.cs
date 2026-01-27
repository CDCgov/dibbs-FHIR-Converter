using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hl7.Fhir.Model;
using Xunit;
using Dibbs.Fhir.Liquid.Converter.DataParsers;
using System;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests
{
    public class ObservationExposureContactTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "Resource",
            "ObservationExposureContact.liquid"
        );

        [Fact]
        public void ObservationExposureContact_Location_AllFields()
        {
            // from 3.1 spec
            var xmlStr = @"
            <observation
                classCode=""OBS""
                moodCode=""EVN""
                xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                xsi:schemaLocation=""urn:hl7-org:v3 ../../../cda-core-2.0/schema/extensions/SDTC/infrastructure/cda/CDA_SDTC.xsd""
                xmlns=""urn:hl7-org:v3""
                xmlns:cda=""urn:hl7-org:v3""
                xmlns:sdtc=""urn:hl7-org:sdtc""
                xmlns:voc=""http://www.lantanagroup.com/voc""
                >
                <!-- [eICR R2 STU3] Exposure/Contact Information Observation-->
                <templateId root=""2.16.840.1.113883.10.20.15.2.3.52"" extension=""2021-01-01""/>
                <id root=""5f2e0ab0-b505-438a-9d50-f22d78ad1567""/>
                <code code=""C3841750""
                    displayName=""Mass gathering""
                    codeSystem=""2.16.840.1.113883.6.1""
                    codeSystemName=""LOINC""/>
                <statusCode code=""completed""/>
                <effectiveTime>
                    <low value=""202011101800""/>
                    <high value=""202011102130""/>
                </effectiveTime>
                <value xsi:type=""CD"" code=""264379009""
                    displayName=""Sports stadium (environment)""
                    codeSystem=""2.16.840.1.113883.6.96""
                    codeSystemName=""SNOMED CT"">
                    <originalText>City Football Stadium</originalText>
                </value>
                <participant typeCode=""LOC"">
                    <!-- [eICR R2 STU3] Location Participant -->
                    <templateId root=""2.16.840.1.113883.10.20.15.2.3.52"" extension=""2021-01-01""/>
                    <participantRole classCode=""TERR"">
                        <addr>
                            <streetAddressLine>99 Football Stadium Road </streetAddressLine>
                            <city>MyCity</city>
                            <state>AZ</state>
                            <postalCode>8562</postalCode>
                            <country>US</country>
                        </addr>
                    </participantRole>
                </participant>
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
            Assert.Equal("Mass gathering", actualFhir.Code?.Coding?.First().Display);
            Assert.Equal("http://loinc.org", actualFhir.Code?.Coding?.First().System);

            Assert.Equal("2020-11-10T18:00:00", (actualFhir.Effective as Period).Start);
            Assert.Equal("2020-11-10T21:30:00", (actualFhir.Effective as Period).End);

            Assert.IsType<CodeableConcept>(actualFhir.Value);
            var value = (CodeableConcept)actualFhir.Value;

            Assert.Equal("264379009", value.Coding.First().Code);
            Assert.Equal("http://snomed.info/sct", value.Coding.First().System);
            Assert.Equal("Sports stadium", value.Coding.First().Display);
            Assert.Equal("City Football Stadium", value.Text);

            var extensions = actualFhir.Extension;
            Assert.Equal(1, extensions.Count());
            Assert.Equal("MyCity", (extensions[0].Value as Address).City);
        }

        [Fact]
        public void ObservationExposureContact_Human_AllFields()
        {
            var xmlPersonStr = @"
            <playingEntity classCode=""PSN"">
                <name use=""L"">
                    <given>Adam</given>
                    <family>Everyman</family>
                </name>
            </playingEntity>
            ";
            // from 3.1 spec
            var xmlStr = @$"
            <observation
                classCode=""OBS""
                moodCode=""EVN""
                xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                xsi:schemaLocation=""urn:hl7-org:v3 ../../../cda-core-2.0/schema/extensions/SDTC/infrastructure/cda/CDA_SDTC.xsd""
                xmlns=""urn:hl7-org:v3""
                xmlns:cda=""urn:hl7-org:v3""
                xmlns:sdtc=""urn:hl7-org:sdtc""
                xmlns:voc=""http://www.lantanagroup.com/voc""
                >
                <!-- [eICR R2 STU3] Exposure/Contact Information Observation-->
                <templateId root=""2.16.840.1.113883.10.20.15.2.3.52"" extension=""2021-01-01"" />
                <id root=""a78adeed-d623-49cb-95df-42a0a2c33fca"" />
                <code code=""PHC2267""
                    displayName=""Contact with known case""
                    codeSystem=""2.16.840.1.114222.4.5.274""
                    codeSystemName=""PHIN VS (CDC Local Coding System)"" />
                <statusCode code=""completed"" />
                <effectiveTime>
                    <low value=""20201109"" />
                    <high value=""20201113"" />
                </effectiveTime>
                <participant typeCode=""IND"">
                    <!-- [eICR R2 STU3] Person Participant -->
                    <templateId root=""2.16.840.1.113883.10.20.15.2.4.6"" extension=""2021-01-01"" />
                    <functionCode code=""AEXPOS""
                        displayName=""acquisition exposure""
                        codeSystem=""2.16.840.1.113883.5.6""
                        codeSystemNem=""HL7ActClass"" />
                    <participantRole>
                        {xmlPersonStr}
                    </participantRole>
                </participant>
                <!-- Possible agent of exposure -->
                <participant typeCode=""CSM"">
                    <participantRole>
                        <playingEntity>
                            <code code=""840533007""
                                displayName=""Severe acute respiratory syndrome coronavirus 2 (organism)""
                                codeSystem=""2.16.840.1.113883.6.96""
                                codeSystemName=""SNOMED CT""/>
                        </playingEntity>
                    </participantRole>
                </participant>
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
            Assert.Equal("Contact with known case", actualFhir.Code?.Coding?.First().Display);
            Assert.Equal("urn:oid:2.16.840.1.114222.4.5.274", actualFhir.Code?.Coding?.First().System);

            Assert.Equal("2020-11-09", (actualFhir.Effective as Period).Start);
            Assert.Equal("2020-11-13", (actualFhir.Effective as Period).End);

            var components = actualFhir.Component;
            Assert.Equal(1, components.Count());
            Assert.Equal("840533007", (components[0].Value as CodeableConcept).Coding[0].Code);

            // Check related person also renders okay
            var parsedPerson = new CcdaDataParser().Parse(xmlPersonStr) as Dictionary<string, object>;
            var personAttributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "relatedPerson", parsedPerson["playingEntity"] }
            };

            var personFhir = GetFhirObjectFromTemplate<RelatedPerson>(
                Path.Join(TestConstants.ECRTemplateDirectory, "Resource", "_RelatedPerson.liquid"),
                personAttributes);

            Assert.Equal(ResourceType.RelatedPerson.ToString(), personFhir.TypeName);
            Assert.NotNull(personFhir.Id);

            Assert.Equal(1, personFhir.Name.Count());
            Assert.Equal("Adam", personFhir.Name[0].Given.First());
            Assert.Equal("Everyman", personFhir.Name[0].Family);
            Assert.Equal("Official", personFhir.Name[0].Use.ToString());
        }

        [Fact]
        public void ObservationExposureContact_Animal_AllFields()
        {
            var xmlAnimalStr = @"
            <value code=""35794008""
                displayName=""Wild mink (organism)""
                codeSystem=""2.16.840.1.113883.6.96""
                codeSystemName=""SNOMED CT""/>
            ";
            // from 3.1 spec
            var xmlStr = @$"
            <observation
                classCode=""OBS""
                moodCode=""EVN""
                xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                xsi:schemaLocation=""urn:hl7-org:v3 ../../../cda-core-2.0/schema/extensions/SDTC/infrastructure/cda/CDA_SDTC.xsd""
                xmlns=""urn:hl7-org:v3""
                xmlns:cda=""urn:hl7-org:v3""
                xmlns:sdtc=""urn:hl7-org:sdtc""
                xmlns:voc=""http://www.lantanagroup.com/voc""
                >
                <!-- [eICR R2 STU3] Exposure/Contact Information Observation-->
                <templateId root=""2.16.840.1.113883.10.20.15.2.3.52"" extension=""2021-01-01""/>
                <id root=""a78adeed-d623-49cb-95df-42a0a2c33fca""/>
                <code code=""PHC2266""
                    displayName=""Animal with confirmed or suspected case""
                    codeSystem=""2.16.840.1.114222.4.5.274""
                    codeSystemName=""PHIN VS (CDC Local Coding System)""/>
                <statusCode code=""completed""/>
                <effectiveTime>
                    <low value=""20201109""/>
                    <high value=""20201113""/>
                </effectiveTime>
                <participant typeCode=""EXPART"">
                    <!-- [eICR R2 STU3] Animal Participant -->
                    <templateId root=""2.16.840.1.113883.10.20.15.2.4.5"" extension=""2021-01-01""/>
                    <functionCode code=""AEXPOS""
                        displayName=""acquisition exposure""
                        codeSystem=""2.16.840.1.113883.5.6""
                        codeSystemNem=""HL7ActClass"" />
                    <participantRole>
                        <playingEntity classCode=""ANM"">
                            {xmlAnimalStr}
                        </playingEntity>
                    </participantRole>
                </participant>
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
            Assert.Equal("Animal with confirmed or suspected case", actualFhir.Code?.Coding?.First().Display);
            Assert.Equal("urn:oid:2.16.840.1.114222.4.5.274", actualFhir.Code?.Coding?.First().System);

            Assert.Equal("2020-11-09", (actualFhir.Effective as Period).Start);
            Assert.Equal("2020-11-13", (actualFhir.Effective as Period).End);

            // Check related person also renders okay
            var parsedAnimal = new CcdaDataParser().Parse(xmlAnimalStr) as Dictionary<string, object>;
            var animalAttributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "animalCode", parsedAnimal["value"] }
            };

            var animalFhir = GetFhirObjectFromTemplate<RelatedPerson>(
                Path.Join(TestConstants.ECRTemplateDirectory, "Resource", "_RelatedPersonAnimal.liquid"),
                animalAttributes);

            Assert.Equal(ResourceType.RelatedPerson.ToString(), animalFhir.TypeName);
            Assert.NotNull(animalFhir.Id);

            Assert.Equal(1, animalFhir.Extension.Count());
            Assert.Equal(
                "http://hl7.org/fhir/StructureDefinition/practitioner-animalSpecies",
                animalFhir.Extension[0].Url);
            var value = (CodeableConcept)animalFhir.Extension[0].Value;
            Assert.Equal("Wild mink", value.Coding[0].Display);
            Assert.Equal("35794008", value.Coding[0].Code);
        }
    }
}
