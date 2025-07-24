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
    public class ObservationLastMenstrualPeriodTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "Resource",
            "_ObservationLastMenstrualPeriod.liquid"
        );

        [Fact]
        public void ObservationLastMenstrualPeriod_AllFields()
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
                    xmlns:voc=""http://www.lantanagroup.com/voc"">
                    <templateId root=""2.16.840.1.113883.10.20.30.3.34"" extension=""2014-06-09""/>
                    <id root=""11f83a4d-344e-4c62-ac97-4ef857616562""/>
                    <code code=""8665-2"" codeSystem=""2.16.840.1.113883.6.1"" codeSystemName=""LOINC""
                        displayName=""Last menstrual period start date""/>
                    <statusCode code=""completed""/>
                    <effectiveTime value=""20121128""/>
                    <value xsi:type=""TS"" value=""20121104""/>
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
            Assert.Equal("Last menstrual period start date", actualFhir.Code?.Coding?.First().Display);
            Assert.Equal("http://loinc.org", actualFhir.Code?.Coding?.First().System);


            Assert.Equal("2012-11-28", (actualFhir.Effective as FhirDateTime)?.Value);

            Assert.Equal("2012-11-04", actualFhir.Value.ToString());
        }
    }
}
