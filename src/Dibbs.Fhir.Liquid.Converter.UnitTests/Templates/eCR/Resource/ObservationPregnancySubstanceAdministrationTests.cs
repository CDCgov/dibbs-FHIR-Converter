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
    public class ObservationPregnancySubstanceAdministrationTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "Resource",
            "ObservationPregnancySubstanceAdministration.liquid"
        );

        [Fact]
        public void PregnancySubstanceAdministration_AllFields()
        {
            var xmlString =
                @"
                <substanceAdministration classCode=""SBADM"" moodCode=""EVN"">
                    <!-- [C-CDA R2.1] Medication Activity (V2) -->
                    <templateId root=""2.16.840.1.113883.10.20.22.4.16"" extension=""2014-06-09"" />
                    <!-- [C-CDA PREG] D Immune Globulin (RhIG) Given -->
                    <templateId root=""2.16.840.1.113883.10.20.22.4.302"" extension=""2018-04-01"" />
                    <id root=""7abe5aa2-c1ba-4552-85fc-796b4c00c50d"" />
                    <statusCode code=""completed"" />
                    <effectiveTime xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""IVL_TS"">
                        <low value=""20170618"" />
                    </effectiveTime>
                    <consumable>
                        <manufacturedProduct classCode=""MANU"">
                            <!-- [C-CDA R2.1] Medication information -->
                            <templateId root=""2.16.840.1.113883.10.20.22.4.23"" extension=""2014-06-09"" />
                            <!-- [C-CDA PREG] D Immune Globulin (RhIG) -->
                            <templateId root=""2.16.840.1.113883.10.20.22.4.303"" extension=""2018-04-01"" />
                            <id root=""8bdb8496-2acb-4ae3-b231-69e4c92e25a1"" />
                            <manufacturedMaterial>
                                <code code=""1790513""
                                    displayName=""13 ML Rho(D) Immune Globulin, human 1154 UNT/ML Injection [WinRho]""
                                    codeSystem=""2.16.840.1.113883.6.88""
                                    codeSystemName=""RxNorm"" />
                            </manufacturedMaterial>
                        </manufacturedProduct>
                    </consumable>
                </substanceAdministration>";

            var parser = new CcdaDataParser();
            var parsedXml = parser.Parse(xmlString) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "observationEntry", parsedXml["substanceAdministration"] },
            };

            var actualFhir = GetFhirObjectFromTemplate<Observation>(ECRPath, attributes);

            Assert.Equal("Observation", actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);
            Assert.Equal(
                "http://hl7.org/fhir/StructureDefinition/Observation",
                actualFhir.Meta.Profile.First()
            );
            Assert.NotEmpty(actualFhir.Identifier);
            Assert.Equal("Final", actualFhir.Status.ToString());

            Assert.Equal("2017-06-18", (actualFhir.Effective as Period)?.Start);

            Assert.Equal("http://loinc.org", actualFhir.Code.Coding.First().System);
            Assert.Equal("73813-8", actualFhir.Code.Coding.First().Code);
            Assert.Equal("Characteristics of labor and delivery [US Standard Certificate of Live Birth]", actualFhir.Code.Coding.First().Display);
            Assert.Equal("Characteristics of labor and delivery", actualFhir.Code.Text);

            Assert.IsType<CodeableConcept>(actualFhir.Value);
            var value = (CodeableConcept)actualFhir.Value;

            Assert.Equal("1790513", value.Coding.First().Code);
            Assert.Equal("http://www.nlm.nih.gov/research/umls/rxnorm", value.Coding.First().System);
            Assert.Equal("13 ML Rho(D) immune globulin, human 1154 UNT/ML Injection [WinRho]", value.Coding.First().Display);
        }
    }
}
