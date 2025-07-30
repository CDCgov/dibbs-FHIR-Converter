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
    public class ObservationExposureContactTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "Resource",
            "_ObservationExposureContact.liquid"
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
            Assert.Equal("Sports stadium (environment)", value.Coding.First().Display);
            Assert.Equal("City Football Stadium", value.Text);
            
            var extensions = actualFhir.Extension;
            Assert.Equal(1, extensions.Count());
            Assert.Equal("MyCity", (extensions[0].Value as Address).City);
        }
    }
}
