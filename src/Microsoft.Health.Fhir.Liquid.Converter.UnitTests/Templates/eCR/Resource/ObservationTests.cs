using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotLiquid;
using Xunit;
using Hl7.Fhir.Model;
using Microsoft.Health.Fhir.Liquid.Converter.Parsers;

namespace Microsoft.Health.Fhir.Liquid.Converter.UnitTests
{
    public class ObservationTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory, "Resource", "_Observation.liquid"
        );

        [Fact]
        public void ObservationLab_Basic_AllFields()
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
                <!-- [C-CDA R1.1] Result Observation -->
                <templateId root=""2.16.840.1.113883.10.20.22.4.2"" />
                <!-- [C-CDA R2.1] Result Observation (V3) -->
                <templateId root=""2.16.840.1.113883.10.20.22.4.2"" extension=""2015-08-01"" />
                <!-- [eICR R2 STU1.1] Initial Case Report Trigger Code Result Observation (V2) -->
                <templateId root=""2.16.840.1.113883.10.20.15.2.3.2"" extension=""2019-04-01"" />
                <id root=""bf9c0a26-4524-4395-b3ce-100450b9c9ad"" />
                <!-- This code is a trigger code from RCTC subset:
                    ""R4 Lab Obs Test Name Triggers for Public Health Reporting (RCTC subset)""
                    @sdtc:valueSet and @sdtc:valueSetVersion shall be present -->
                <code code=""11585-7""
                    codeSystem=""2.16.840.1.113883.6.1""
                    codeSystemName=""LOINC""
                    displayName=""Bordetella pertussis Ab [Units/volume] in Serum""
                    sdtc:valueSet=""2.16.840.1.114222.4.11.7508""
                    sdtc:valueSetVersion=""2020-11-13"" />
                <!-- statusCode is set to completed indicating that this is a final result -->
                <statusCode code=""completed"" />
                <effectiveTime value=""20201207"" />
                <!-- This value is a physical quantity and thus cannot be a trigger code -->
                <value xsi:type=""PQ"" unit=""[iU]/mL"" value=""100"" />
                <!-- This interpretation code denotes that this patient value is above high normal -->
                <interpretationCode code=""H""
                    displayName=""High""
                    codeSystem=""2.16.840.1.113883.5.83""
                    codeSystemName=""ObservationInterpretation"" />
                <referenceRange>
                    <observationRange>
                        <!-- Reference range: PT IgG: <45 IU/mL -->
                        <value xsi:type=""IVL_PQ"">
                        <high inclusive=""false"" unit=""[iU]/mL"" value=""45"" />
                        </value>
                        <!-- This interpretation code denotes that this reference range is for normal
                        results.
                        This is not the interpretation of a specific patient value-->
                        <interpretationCode code=""N"" codeSystem=""2.16.840.1.113883.5.83"" displayName=""Normal""
                        />
                    </observationRange>
                </referenceRange>
                <text>
                    <reference value=""#ipointtohtml""/>
                </text>
            </observation>
            ";
            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "observationCategory", "laboratory" },
                { "observationEntry", parsed["observation"]},
            };

            var actualFhir = GetFhirObjectFromTemplate<Observation>(ECRPath, attributes);

            Assert.Equal(ResourceType.Observation.ToString(), actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);

            Assert.Equal(ObservationStatus.Final, actualFhir.Status);

            Assert.NotNull(actualFhir.Code);
            Assert.Equal("Bordetella pertussis Ab [Units/volume] in Serum", actualFhir.Code?.Coding?.First().Display);
            Assert.Equal("http://loinc.org", actualFhir.Code?.Coding?.First().System);


            Assert.Equal("2020-12-07", (actualFhir.Effective as FhirDateTime)?.Value);

            Assert.Equal("45", actualFhir.ReferenceRange.First().High.Value.ToString());
            Assert.Equal("N", actualFhir.ReferenceRange.First().Type.Coding.First().Code);
            Assert.Equal("H", actualFhir.Interpretation.First().Coding.First().Code);

            Assert.IsType<Quantity>(actualFhir.Value);
            var value = (Quantity)actualFhir.Value;

            Assert.Equal("100", value.Value.ToString());
            Assert.Equal("[iU]/mL", value.Unit);

            Assert.Equal(
                "http://hl7.org/fhir/us/ecr/StructureDefinition/us-ph-lab-result-observation",
                actualFhir.Meta.Profile.First());
            Assert.Equal(
                "laboratory",
                actualFhir.Category.First().Coding.First().Code);

            var refExt = actualFhir.Extension.First();
            Assert.Equal("observation entry reference value", refExt.Url);
            Assert.Equal("#ipointtohtml", refExt.Value.ToString());
        }

        [Fact]
        public void ObservationVitalSign_Basic_AllFields()
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
                <templateId root=""2.16.840.1.113883.10.20.22.4.27"" extension=""2014-06-09"" />
                <!-- Vital Sign Observation template -->
                <id root=""c6f88321-67ad-11db-bd13-0800200c9a66"" />
                <code code=""8302-2"" codeSystem=""2.16.840.1.113883.6.1"" codeSystemName=""LOINC""
                displayName=""Height"" />
                <statusCode code=""completed"" />
                <effectiveTime value=""20121114"" />
                <value xsi:type=""PQ"" value=""177"" unit=""cm"" />
            </observation>
            ";
            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "observationCategory", "vital-signs" },
                { "observationEntry", parsed["observation"]},
            };

            var actualFhir = GetFhirObjectFromTemplate<Observation>(ECRPath, attributes);

            Assert.Equal(ResourceType.Observation.ToString(), actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);

            Assert.Equal(ObservationStatus.Final, actualFhir.Status);

            Assert.NotNull(actualFhir.Code);
            Assert.Equal("Height", actualFhir.Code?.Coding?.First().Display);
            Assert.Equal("http://loinc.org", actualFhir.Code?.Coding?.First().System);


            Assert.Equal("2012-11-14", (actualFhir.Effective as FhirDateTime)?.Value);


            Assert.IsType<Quantity>(actualFhir.Value);
            var value = (Quantity)actualFhir.Value;

            Assert.Equal("177", value.Value.ToString());
            Assert.Equal("cm", value.Unit);

            Assert.Equal(
                "http://hl7.org/fhir/StructureDefinition/Observation",
                actualFhir.Meta.Profile.First());
            Assert.Equal(
                "vital-signs",
                actualFhir.Category.First().Coding.First().Code);

            Assert.Equal(0, actualFhir.Extension.Count());
        }


        [Fact]
        public void Obs_Status_GivenLabObsResultStatus_ReturnsStatusFromObs()
        {
            var attributes = new Dictionary<string, object>
            { { "ID", "123" }, { "observationEntry", Hash.FromAnonymousObject(new { entryRelationship = new { observation = new { templateId = new { root = "2.16.840.1.113883.10.20.22.4.419" }, value = new { code = "P", displayName = "Preliminary results", codeSystem = "2.16.840.1.113883.18.34", codeSystemName = "HL7ObservationResultStatusCodesInterpretation" } } } }) } };

            var actualContent = RenderLiquidTemplate(ECRPath, attributes);
            Assert.Contains(@"""status"":""preliminary""", actualContent);
            Assert.Contains(@"""url"": ""http://terminology.hl7.org/ValueSet/v2-0085""", actualContent);
        }

        [Fact]
        public void Obs_Status_GivenNoLabObsResultStatus_ReturnsStatusFromStatusCode()
        {
            var attributes = new Dictionary<string, object>
            { { "ID", "123" }, { "observationEntry", Hash.FromAnonymousObject(new { statusCode = new { code = "active" } }) } };

            var actualContent = RenderLiquidTemplate(ECRPath, attributes);
            Assert.Contains(@"""status"":""preliminary""", actualContent);
            Assert.DoesNotContain(@"""url"": ""http://terminology.hl7.org/ValueSet/v2-0085""", actualContent);
        }
    }
}
