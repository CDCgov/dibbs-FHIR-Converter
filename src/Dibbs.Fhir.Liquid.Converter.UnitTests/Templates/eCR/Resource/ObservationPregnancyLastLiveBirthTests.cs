using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hl7.Fhir.ElementModel.Types;
using Hl7.Fhir.Model;
using Dibbs.Fhir.Liquid.Converter.DataParsers;
using Namotion.Reflection;
using Xunit;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests
{
    public class ObservationPregnancyLastLiveBirthTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "Resource",
            "ObservationPregnancyLastLiveBirth.liquid"
        );

        [Fact]
        public void PregnancyLastLiveBirth_AllFields()
        {
            var xmlString =
                @"
                <observation classCode=""OBS"" moodCode=""EVN"">
                    <!-- [C-CDA PREG] Date of Last Live Birth -->
                    <templateId root=""2.16.840.1.113883.10.20.22.4.294"" extension=""2018-04-01"" />
                    <id root=""2457808c-ec58-40f6-9e06-eef765794c28"" />
                    <code code=""68499-3""
                        displayName=""Date of last live birth""
                        codeSystem=""2.16.840.1.113883.6.1""
                        codeSystemName=""LOINC"" />
                    <statusCode code=""completed"" />
                    <effectiveTime value=""201801051015"" />
                    <value xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""TS"" value=""20160501"" />
                </observation>";

            var parser = new CcdaDataParser();
            var parsedXml = parser.Parse(xmlString) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "observationEntry", parsedXml["observation"] },
            };

            var actualFhir = GetFhirObjectFromTemplate<Observation>(ECRPath, attributes);

            Assert.Equal("Observation", actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);
            Assert.Equal(
                "http://hl7.org/fhir/us/bfdr/StructureDefinition/Observation-date-of-last-live-birth",
                actualFhir.Meta.Profile.First()
            );
            Assert.NotEmpty(actualFhir.Identifier);
            Assert.Equal("Final", actualFhir.Status.ToString());

            Assert.Equal("http://loinc.org", actualFhir.Code.Coding.First().System);
            Assert.Equal("68499-3", actualFhir.Code.Coding.First().Code);
            Assert.Equal("Date last live birth", actualFhir.Code.Coding.First().Display);

            Assert.Equal("2016-05-01", (actualFhir.Value as FhirDateTime)?.Value);
            Assert.Equal("2018-01-05T10:15:00", (actualFhir.Effective as FhirDateTime)?.Value);
        }
    }
}
