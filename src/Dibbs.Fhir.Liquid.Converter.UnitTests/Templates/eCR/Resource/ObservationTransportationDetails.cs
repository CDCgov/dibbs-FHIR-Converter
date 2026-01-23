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
    public class ObservationTransportationDetailsTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "Resource",
            "_ObservationTransportationDetails.liquid"
        );

        [Fact]
        public void ObservationTransportationDetails_Location_AllFields()
        {
            // from 3.1 spec
            var xmlStr = @"
            <organizer 
                classCode=""OBS"" 
                moodCode=""EVN""
                xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                xsi:schemaLocation=""urn:hl7-org:v3 ../../../cda-core-2.0/schema/extensions/SDTC/infrastructure/cda/CDA_SDTC.xsd""
                xmlns=""urn:hl7-org:v3""
                xmlns:cda=""urn:hl7-org:v3""
                xmlns:sdtc=""urn:hl7-org:sdtc""
                xmlns:voc=""http://www.lantanagroup.com/voc""
                >
                <!-- [eICR R2 STU3] Transportation Details Organizer -->
                <templateId root=""2.16.840.1.113883.10.20.15.2.3.50"" extension=""2021-01-01""/>
                <id root=""afd5c96f-9289-4796-b8fa-faa50ac113ff""/>
                <!-- Transport vehicle type -->
                <code code=""21812002""
                    codeSystem=""2.16.840.1.113883.6.1""
                    displayName=""Ocean liner, device (physical object)""
                    codeSystemName=""SNOMED CT""/>
                <statusCode code=""completed""/>
                <effectiveTime>
                    <low value=""20200425""/>
                    <high value=""20200515""/>
                </effectiveTime>
                <component>
                    <observation classCode=""OBS"" moodCode=""EVN"">
                        <!-- [eICR R2 STU3] Transportation Details Observation -->
                        <templateId root=""2.16.840.1.113883.10.20.15.2.3.49"" extension=""2021-01-01""/>
                        <id root=""1679fc35-4cbf-48d5-8998-9d5bfd70fcc2""/>
                        <code nullFlavor=""OTH"">
                            <originalText>Ship Name</originalText>
                        </code>
                        <statusCode code=""completed""/>
                        <effectiveTime>
                            <low value=""20200425""/>
                            <high value=""20200515""/>
                        </effectiveTime>
                        <value xsi:type=""ST"">Princess of the Sea</value>
                    </observation>
                </component>
            </organizer>
            ";
            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "transitOrganizer", parsed["organizer"]},
            };

            var actualFhir = GetFhirObjectFromTemplate<Observation>(ECRPath, attributes);

            Assert.Equal(ResourceType.Observation.ToString(), actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);

            Assert.Equal(ObservationStatus.Final, actualFhir.Status);

            Assert.NotNull(actualFhir.Code);
            Assert.Equal("Transportation details", actualFhir.Code?.Coding?.First().Display);
            Assert.Equal("http://snomed.info/sct", actualFhir.Code?.Coding?.First().System);

            Assert.NotNull(actualFhir.Value);
            Assert.Equal(
                "Ocean liner, device (physical object)",
                (actualFhir.Value as CodeableConcept)?.Coding?.First().Display);

            Assert.Equal("2020-04-25", (actualFhir.Effective as Period).Start);
            Assert.Equal("2020-05-15", (actualFhir.Effective as Period).End);

            var components = actualFhir.Component;
            Assert.Equal(1, components.Count());
            Assert.Equal("Ship Name", (components[0].Code as CodeableConcept).Text);
            Assert.Equal("Princess of the Sea", (components[0].Value as FhirString).ToString());
        }
    }
}
