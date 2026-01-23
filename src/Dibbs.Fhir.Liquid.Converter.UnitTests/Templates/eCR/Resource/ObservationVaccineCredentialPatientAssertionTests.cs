using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotLiquid;
using Hl7.Fhir.Model;
using Microsoft.Health.Fhir.Liquid.Converter.Parsers;
using Xunit;

namespace Microsoft.Health.Fhir.Liquid.Converter.UnitTests
{
    public class ObservationVaccineCredentialPatientAssertionTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory, "Resource", "_ObservationVaccineCredentialPatientAssertion.liquid"
        );

        [Fact]
        public void allFields()
        {
            var xmlStr = @"<observation
                xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                xsi:schemaLocation=""urn:hl7-org:v3 ../../../cda-core-2.0/schema/extensions/SDTC/infrastructure/cda/CDA_SDTC.xsd""
                xmlns=""urn:hl7-org:v3""
                xmlns:cda=""urn:hl7-org:v3""
                xmlns:sdtc=""urn:hl7-org:sdtc""
                xmlns:voc=""http://www.lantanagroup.com/voc""
                classCode=""OBS""
                moodCode=""EVN"">
                <!-- [eICR R2 STU3] Vaccine Credential Patient Assertion -->
                <templateId root=""2.16.840.1.113883.10.20.15.2.3.55""
                    extension=""2021-01-01"" />
                <id root=""3adc7f4d-ff0a-4b38-9252-bd65edbe3dff"" />
                <code code=""11370-4"" displayName=""Immunization status - Reported""
                    codeSystem=""2.16.840.1.113883.6.1"" codeSystemName=""LOINC"" />
                <statusCode code=""completed"" />
                <effectiveTime value=""20201107"" />
                <value xsi:type=""CD"" code=""Y"" codeSystem=""2.16.840.1.113883.12.136""
                    displayName=""Yes"" codeSystemName=""Yes/No Indicator (HL7 Table 0136)"" />
            </observation>";

            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "observationEntry", parsed["observation"]},
            };

            var actualFhir = GetFhirObjectFromTemplate<Observation>(ECRPath, attributes);

            Assert.NotNull(actualFhir.Id);
            Assert.NotEmpty(actualFhir.Identifier);
            Assert.Equal("Final", actualFhir.Status.ToString());
            Assert.Equal("11370-4", actualFhir.Code.Coding.First().Code);
            Assert.NotNull(actualFhir.Effective);
            Assert.NotEmpty((actualFhir.Value as CodeableConcept).Coding);
        }
    }
}
