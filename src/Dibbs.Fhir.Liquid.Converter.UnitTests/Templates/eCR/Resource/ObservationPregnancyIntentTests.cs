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
    public class ObservationPregnancyIntentTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "Resource",
            "ObservationPregnancyIntent.liquid"
        );

        [Fact]
        public void PregnancyIntent_AllFields()
        {
            var xmlString =
                @"
                <observation classCode=""OBS"" moodCode=""INT"">
                    <!-- [C-CDA PREG] Pregnancy Intention in Next Year -->
                    <templateId root=""2.16.840.1.113883.10.20.22.4.281"" extension=""2018-04-01"" />
                    <id root=""b06f4c63-5d18-48d3-9c75-f10bba135525"" />
                    <code code=""86645-9"" codeSystem=""2.16.840.1.113883.6.1"" codeSystemName=""LOINC""
                        displayName=""Future pregnancy intention Reported""/>
                    <statusCode code=""completed""/>
                    <effectiveTime>
                        <low value=""20170107""/>
                        <!-- High value should be 1 year from low date -->
                        <high value=""20180107""/>
                    </effectiveTime>
                    <value xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""CD"" code=""454401000124105""
                        codeSystem=""2.16.840.1.113883.6.96""
                        codeSystemName=""SNOMED CT""
                        displayName=""No desire to become pregnant""/>
                </observation>";

            var parser = new CcdaDataParser();
            var parsedXml = parser.Parse(xmlString) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "observationCategory", "social-history" },
                { "observationEntry", parsedXml["observation"] },
            };

            var actualFhir = GetFhirObjectFromTemplate<Observation>(ECRPath, attributes);

            Assert.Equal("Observation", actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);
            Assert.Equal(
                "http://hl7.org/fhir/us/core/StructureDefinition/us-core-observation-pregnancyintent",
                actualFhir.Meta.Profile.First()
            );
            Assert.NotEmpty(actualFhir.Identifier);
            Assert.Equal("Final", actualFhir.Status.ToString());
            Assert.Equal("http://loinc.org", actualFhir.Code.Coding.First().System);
            Assert.Equal("86645-9", actualFhir.Code.Coding.First().Code);
            Assert.Equal("Pregnancy intention in the next year Reported Nom", actualFhir.Code.Coding.First().Display);

            Assert.Equal("2017-01-07", (actualFhir.Effective as Period).Start);
            Assert.Equal("2018-01-07", (actualFhir.Effective as Period).End);

            Assert.IsType<CodeableConcept>(actualFhir.Value);
            var value = (CodeableConcept)actualFhir.Value;

            Assert.Equal("454401000124105", value.Coding.First().Code);
            Assert.Equal("http://snomed.info/sct", value.Coding.First().System);
            Assert.Equal("No desire to become pregnant", value.Coding.First().Display);
        }
    }
}
