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
    public class ObservationTravelHistoryTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "Resource",
            "_ObservationTravelHistory.liquid"
        );

        [Fact]
        public void ObservationTravelHistory_Address()
        {
            // from 3.1 spec
            var xmlStr = @"
            <act 
                classCode=""ACT"" 
                moodCode=""EVN""
                xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                xsi:schemaLocation=""urn:hl7-org:v3 ../../../cda-core-2.0/schema/extensions/SDTC/infrastructure/cda/CDA_SDTC.xsd""
                xmlns=""urn:hl7-org:v3""
                xmlns:cda=""urn:hl7-org:v3""
                xmlns:sdtc=""urn:hl7-org:sdtc""
                xmlns:voc=""http://www.lantanagroup.com/voc""
                >
                <!-- Where a more granular address than state is known (e.g.: city, street)
                it is appropriate to use address rather than the coded location. -->
                <!-- [eICR R2 STU1.1] Travel History -->
                <templateId root=""2.16.840.1.113883.10.20.15.2.3.1"" extension=""2022-05-01"" />
                <id root=""37c76c51-6411-4e1d-8a37-957fd49d2cda"" />
                <code code=""420008001""
                    displayName=""Travel""
                    codeSystem=""2.16.840.1.113883.6.96""
                    codeSystemName=""SNOMED CT"" />
                <statusCode code=""completed"" />
                <!-- Duration -->
                <effectiveTime>
                    <low value=""20161022"" />
                    <high value=""20161030"" />
                </effectiveTime>
                <participant typeCode=""LOC"">
                    <participantRole classCode=""TERR"">
                        <addr>
                        <streetAddressLine>1170 N Rancho Robles Rd</streetAddressLine>
                        <city>Oracle</city>
                        <state>AZ</state>
                        <postalCode>8562</postalCode>
                        <country>US</country>
                        </addr>
                    </participantRole>
                </participant>
            </act>
            ";
            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "observationEntry", parsed["act"]},
            };

            var actualFhir = GetFhirObjectFromTemplate<Observation>(ECRPath, attributes);

            Assert.Equal(ResourceType.Observation.ToString(), actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);

            Assert.Equal(ObservationStatus.Final, actualFhir.Status);

            Assert.Equal("420008001", actualFhir.Code?.Coding?.First().Code);

            Assert.Equal("2016-10-22", (actualFhir.Effective as Period)?.Start);
            Assert.Equal("2016-10-30", (actualFhir.Effective as Period)?.End);

            var components = actualFhir.Component;
            Assert.Equal(1, components.Count());
            Assert.Equal("LOC", components[0].Code.Coding[0].Code);
            Assert.Equal("Oracle", (components[0].Extension[0].Value as Address).City);
        }

        [Fact]
        public void ObservationTravelHistory_CodedLocation()
        {
            // from 3.1 spec
            var xmlStr = @"
            <act 
                classCode=""ACT"" 
                moodCode=""EVN""
                xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                xsi:schemaLocation=""urn:hl7-org:v3 ../../../cda-core-2.0/schema/extensions/SDTC/infrastructure/cda/CDA_SDTC.xsd""
                xmlns=""urn:hl7-org:v3""
                xmlns:cda=""urn:hl7-org:v3""
                xmlns:sdtc=""urn:hl7-org:sdtc""
                xmlns:voc=""http://www.lantanagroup.com/voc""
                >
                <!-- [eICR R2 STU1.1] Travel History -->
                <templateId root=""2.16.840.1.113883.10.20.15.2.3.1"" extension=""2022-05-01"" />
                <id root=""37f76f51-6411-4f1d-8a37-957fd49d2add"" />
                <code displayName=""Travel""
                    code=""420008001""
                    codeSystem=""2.16.840.1.113883.6.96""
                    codeSystemName=""SNOMED CT"" />
                <statusCode code=""completed"" />
                <!-- Denotes ""past 3 weeks"" with the high value
                    being the date the statement was made -->
                <effectiveTime>
                    <width value=""3"" unit=""weeks"" />
                    <high value=""20161109"" />
                </effectiveTime>
                <participant typeCode=""LOC"">
                    <participantRole classCode=""TERR"">
                        <!-- Code specifying the location traveled -->
                        <code code=""BRA""
                            displayName=""Brazil""
                            codeSystem=""1.0.3166.1""
                            codeSystemName=""Country (ISO 3166-1)"" />
                    </participantRole>
                </participant>
            </act>
            ";
            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "observationEntry", parsed["act"]},
            };

            var actualFhir = GetFhirObjectFromTemplate<Observation>(ECRPath, attributes);

            Assert.Equal(ResourceType.Observation.ToString(), actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);

            Assert.Equal(ObservationStatus.Final, actualFhir.Status);

            Assert.Equal("420008001", actualFhir.Code?.Coding?.First().Code);

            Assert.Equal("2016-10-19", (actualFhir.Effective as Period)?.Start);
            Assert.Equal("2016-11-09", (actualFhir.Effective as Period)?.End);

            var components = actualFhir.Component;
            Assert.Equal(1, components.Count());
            Assert.Equal("LOC", components[0].Code.Coding[0].Code);
            Assert.Equal("Brazil", (components[0].Value as CodeableConcept).Coding[0].Display);
        }

        [Fact]
        public void ObservationTravelHistory_TextLocation()
        {
            // from 3.1 spec
            var xmlStr = @"
            <act 
                classCode=""ACT"" 
                moodCode=""EVN""
                xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                xsi:schemaLocation=""urn:hl7-org:v3 ../../../cda-core-2.0/schema/extensions/SDTC/infrastructure/cda/CDA_SDTC.xsd""
                xmlns=""urn:hl7-org:v3""
                xmlns:cda=""urn:hl7-org:v3""
                xmlns:sdtc=""urn:hl7-org:sdtc""
                xmlns:voc=""http://www.lantanagroup.com/voc""
                >
                <!-- [eICR R2 STU1.1] Travel History -->
                <templateId root=""2.16.840.1.113883.10.20.15.2.3.1"" extension=""2022-05-01"" />
                <id root=""37f76f51-6411-4e1d-8a37-957fd49d2ade"" />
                <code code=""420008001""
                    displayName=""Travel""
                    codeSystem=""2.16.840.1.113883.6.96""
                    codeSystemName=""SNOMED CT"" />
                <text>Spent 8 years in the UK during the BSE outbreak</text>
                <statusCode code=""completed"" />
                <!-- Duration (from 1999 to 2007) -->
                <effectiveTime>
                    <low value=""1999"" />
                    <high value=""2007"" />
                </effectiveTime>
            </act>
            ";
            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "observationEntry", parsed["act"]},
            };

            var actualFhir = GetFhirObjectFromTemplate<Observation>(ECRPath, attributes);

            Assert.Equal(ResourceType.Observation.ToString(), actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);

            Assert.Equal(ObservationStatus.Final, actualFhir.Status);

            Assert.Equal("420008001", actualFhir.Code?.Coding?.First().Code);

            Assert.Equal("1999", (actualFhir.Effective as Period)?.Start);
            Assert.Equal("2007", (actualFhir.Effective as Period)?.End);

            var components = actualFhir.Component;
            Assert.Equal(1, components.Count());
            Assert.Equal("LOC", components[0].Code.Coding[0].Code);
            Assert.Equal("Spent 8 years in the UK during the BSE outbreak", (components[0].Value as CodeableConcept).Text);
        }

                [Fact]
        public void ObservationTravelHistory_ReferenceTextLocation()
        {
            // from 3.1 spec
            var xmlStr = @"
            <act 
                classCode=""ACT"" 
                moodCode=""EVN""
                xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                xsi:schemaLocation=""urn:hl7-org:v3 ../../../cda-core-2.0/schema/extensions/SDTC/infrastructure/cda/CDA_SDTC.xsd""
                xmlns=""urn:hl7-org:v3""
                xmlns:cda=""urn:hl7-org:v3""
                xmlns:sdtc=""urn:hl7-org:sdtc""
                xmlns:voc=""http://www.lantanagroup.com/voc""
                >
                <!-- [eICR R2 STU1.1] Travel History -->
                <templateId root=""2.16.840.1.113883.10.20.15.2.3.1"" extension=""2022-05-01"" />
                <id root=""37f76f51-6411-4e1d-8a37-957fd49d2ade"" />
                <code code=""420008001""
                    displayName=""Travel""
                    codeSystem=""2.16.840.1.113883.6.96""
                    codeSystemName=""SNOMED CT"" />
                <text><reference value=""#trvhst-01"" /></text>
                <statusCode code=""completed"" />
                <!-- Duration (from 1999 to 2007) -->
                <effectiveTime>
                    <low value=""1999"" />
                    <high value=""2007"" />
                </effectiveTime>
            </act>
            ";
            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "socialHistoryText", @"
                <table>
                    <tr id=""trvhst-01"">Somewhere cool</tr>
                </table>
                " },
                { "observationEntry", parsed["act"]},
            };

            var actualFhir = GetFhirObjectFromTemplate<Observation>(ECRPath, attributes);

            Assert.Equal(ResourceType.Observation.ToString(), actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);

            Assert.Equal(ObservationStatus.Final, actualFhir.Status);

            Assert.Equal("420008001", actualFhir.Code?.Coding?.First().Code);

            Assert.Equal("1999", (actualFhir.Effective as Period)?.Start);
            Assert.Equal("2007", (actualFhir.Effective as Period)?.End);

            var components = actualFhir.Component;
            Assert.Equal(1, components.Count());
            Assert.Equal("LOC", components[0].Code.Coding[0].Code);
            Assert.Equal("Somewhere cool", (components[0].Value as CodeableConcept).Text);
        }

        [Fact]
        public void ObservationTravelHistory_PurposeOfTravel()
        {
            // from 3.1 spec
            var xmlStr = @"
            <act 
                classCode=""ACT"" 
                moodCode=""EVN""
                xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                xsi:schemaLocation=""urn:hl7-org:v3 ../../../cda-core-2.0/schema/extensions/SDTC/infrastructure/cda/CDA_SDTC.xsd""
                xmlns=""urn:hl7-org:v3""
                xmlns:cda=""urn:hl7-org:v3""
                xmlns:sdtc=""urn:hl7-org:sdtc""
                xmlns:voc=""http://www.lantanagroup.com/voc""
                >
                <!-- [eICR R2 STU1.1] Travel History -->
                <templateId root=""2.16.840.1.113883.10.20.15.2.3.1"" extension=""2022-05-01"" />
                <id root=""37f76f51-6411-4e1d-8a37-957fd49d2ade"" />
                <code code=""420008001""
                    displayName=""Travel""
                    codeSystem=""2.16.840.1.113883.6.96""
                    codeSystemName=""SNOMED CT"" />
                <statusCode code=""completed"" />
                <!-- Duration (from 1999 to 2007) -->
                <effectiveTime>
                    <low value=""1999"" />
                    <high value=""2007"" />
                </effectiveTime>
                <entryRelationship typeCode=""COMP"">
                    <observation classCode=""OBS"" moodCode=""EVN"">
                    <!-- [eICR R2 STU3] Purpose of Travel Observation -->
                    <templateId root=""2.16.840.1.113883.10.20.15.2.3.51"" extension=""2022-05-01""/>
                    <id root=""3adc7f4d-ff0a-4b38-9252-bd65edbe3dff""/>
                    <code code=""280147009""
                        codeSystem=""2.16.840.1.113883.6.96""
                        displayName=""Type of activity""
                        codeSystemName=""SNOMED CT"" />
                    <statusCode code=""completed""/>
                    <value xsi:type=""CD"" code=""C0683587""
                        codeSystem=""2.16.840.1.113883.6.86""
                        displayName=""Tourism""
                        codeSystemName=""UMLS""/>
                    </observation>
                </entryRelationship>
            </act>
            ";
            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "observationEntry", parsed["act"]},
            };

            var actualFhir = GetFhirObjectFromTemplate<Observation>(ECRPath, attributes);

            Assert.Equal(ResourceType.Observation.ToString(), actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);

            Assert.Equal(ObservationStatus.Final, actualFhir.Status);

            Assert.Equal("420008001", actualFhir.Code?.Coding?.First().Code);

            Assert.Equal("1999", (actualFhir.Effective as Period)?.Start);
            Assert.Equal("2007", (actualFhir.Effective as Period)?.End);

            var components = actualFhir.Component;
            Assert.Equal(1, components.Count());
            Assert.Equal("280147009", components[0].Code.Coding[0].Code);
            Assert.Equal("Tourism", (components[0].Value as CodeableConcept).Coding[0].Display);
        }
    }
}
