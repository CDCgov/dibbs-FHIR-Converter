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
    public class ObservationPregnancySupplementalDataTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "Resource",
            "ObservationPregnancySupplementalData.liquid"
        );

        [Fact]
        public void PregnancyPlurality_AllFields()
        {
            var xmlString =
                @"
                <observation classCode=""OBS"" moodCode=""EVN"">
                    <!-- [C-CDA PREG] Pregnancy Plurality (SUPPLEMENTAL
                    PREGNANCY) -->
                    <!-- The number of fetuses delivered live or dead at any time in the
                    pregnancy regardless of gestational age or if the fetuses were
                    delivered at different dates in the pregnancy. -->
                    <templateId root=""2.16.840.1.113883.10.20.22.4.286""
                        extension=""2018-04-01"" />
                    <id root=""be98df14-e791-4cd7-a6b3-9c2de4a54bb5"" />
                    <code code=""57722-1"" codeSystem=""2.16.840.1.113883.6.1""
                        displayName=""Birth plurality of Pregnancy""
                        codeSystemName=""LOINC"" />
                    <statusCode code=""completed"" />
                    <!-- Observation date -->
                    <effectiveTime value=""201710011015"" />
                    <!-- Plurality -->
                    <value xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""INT"" value=""2"" />
                </observation>";

            var parser = new CcdaDataParser();
            var parsedXml = parser.Parse(xmlString) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "templateId", "2.16.840.1.113883.10.20.22.4.286" },
                { "observationEntry", parsedXml["observation"] },
            };

            var actualFhir = GetFhirObjectFromTemplate<Observation>(ECRPath, attributes);

            Assert.Equal("Observation", actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);
            Assert.Equal(
                "http://hl7.org/fhir/us/bfdr/StructureDefinition/Observation-birth-plurality-of-pregnancy",
                actualFhir.Meta.Profile.First()
            );
            Assert.NotEmpty(actualFhir.Identifier);
            Assert.Equal("Final", actualFhir.Status.ToString());
            Assert.Equal("http://loinc.org", actualFhir.Code.Coding.First().System);
            Assert.Equal("57722-1", actualFhir.Code.Coding.First().Code);
            Assert.Equal("Birth plurality of Pregnancy", actualFhir.Code.Coding.First().Display);

            Assert.Equal("2017-10-01T10:15:00", (actualFhir.Effective as FhirDateTime)?.Value);

            var fhirInteger = actualFhir.Value as Hl7.Fhir.Model.Integer;
            Assert.NotNull(fhirInteger);
            Assert.Equal(2, fhirInteger.Value);
        }

        [Fact]
        public void PregnancyDateOfFirstPrenatalCareVisit_AllFields()
        {
            var xmlString =
                @"
                <observation classCode=""OBS"" moodCode=""EVN"">
                    <!-- [C-CDA PREG] Date of First Prenatal Care Visit for This
                    Pregnancy -->
                    <templateId root=""2.16.840.1.113883.10.20.22.4.295""
                        extension=""2018-04-01"" />
                    <id root=""0d72d0a1-e0fb-4296-873d-fec064f181b1"" />
                    <code code=""69044-6""
                        codeSystem=""2.16.840.1.113883.6.1""
                        displayName=""Date of first prenatal care visit""
                        codeSystemName=""LOINC"" />
                    <statusCode code=""completed"" />
                    <effectiveTime value=""201701071015"" />
                    <!-- Date of first visit -->
                    <value xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""TS"" value=""20160901"" />
                </observation>";

            var parser = new CcdaDataParser();
            var parsedXml = parser.Parse(xmlString) as Dictionary<string, object>;

             var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "templateId", "2.16.840.1.113883.10.20.22.4.295" },
                { "observationEntry", parsedXml["observation"] },
            };

            var actualFhir = GetFhirObjectFromTemplate<Observation>(ECRPath, attributes);

            Assert.Equal("Observation", actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);
            Assert.Equal(
                "http://hl7.org/fhir/us/bfdr/StructureDefinition/Observation-date-of-first-prenatal-care-visit",
                actualFhir.Meta.Profile.First()
            );
            Assert.NotEmpty(actualFhir.Identifier);
            Assert.Equal("Final", actualFhir.Status.ToString());
            Assert.Equal("http://loinc.org", actualFhir.Code.Coding.First().System);
            Assert.Equal("69044-6", actualFhir.Code.Coding.First().Code);
            Assert.Equal("Date first prenatal visit", actualFhir.Code.Coding.First().Display);

            Assert.Equal("2017-01-07T10:15:00", (actualFhir.Effective as FhirDateTime)?.Value);
            Assert.Equal("2016-09-01", (actualFhir.Value as FhirDateTime)?.Value);
        }

        [Fact]
        public void PregnancyTotalPrenatalVisits_AllFields()
        {
            var xmlString =
                @"
                <observation classCode=""OBS"" moodCode=""EVN"">
                    <!-- [C-CDA PREG] Total Number of Prenatal Care Visits for This
                    Pregnancy -->
                    <templateId root=""2.16.840.1.113883.10.20.22.4.296""
                        extension=""2018-04-01"" />
                    <id root=""20c90fe7-c919-4135-8e7c-8aeb2f465054"" />
                    <code code=""68493-6""
                        codeSystem=""2.16.840.1.113883.6.1""
                        displayName=""Total number of prenatal visits for this pregnancy""
                        codeSystemName=""LOINC"" />
                    <statusCode code=""completed"" />
                    <effectiveTime value=""201701071015"" />
                    <!-- Number of visits-->
                    <value xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""INT"" value=""3"" />
                </observation>";

            var parser = new CcdaDataParser();
            var parsedXml = parser.Parse(xmlString) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "templateId", "2.16.840.1.113883.10.20.22.4.296" },
                { "observationEntry", parsedXml["observation"] },
            };

            var actualFhir = GetFhirObjectFromTemplate<Observation>(ECRPath, attributes);

            Assert.Equal("Observation", actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);
            Assert.Equal(
                "http://hl7.org/fhir/us/bfdr/StructureDefinition-Observation-number-prenatal-visits.html",
                actualFhir.Meta.Profile.First()
            );
            Assert.NotEmpty(actualFhir.Identifier);
            Assert.Equal("Final", actualFhir.Status.ToString());
            Assert.Equal("http://loinc.org", actualFhir.Code.Coding.First().System);
            Assert.Equal("68493-6", actualFhir.Code.Coding.First().Code);
            Assert.Equal("Prenatal visits for this pregnancy #", actualFhir.Code.Coding.First().Display);

            Assert.Equal("2017-01-07T10:15:00", (actualFhir.Effective as FhirDateTime)?.Value);

            var fhirInteger = actualFhir.Value as Hl7.Fhir.Model.Integer;
            Assert.NotNull(fhirInteger);
            Assert.Equal(3, fhirInteger.Value);
        }

        [Fact]
        public void PregnancyRelatedFinding_AllFields()
        {
            var xmlString =
                @"
                <observation classCode=""OBS"" moodCode=""EVN"">
                    <!-- [C-CDA R2.1] Problem Observation (V3) -->
                    <templateId root=""2.16.840.1.113883.10.20.22.4.4""
                        extension=""2015-08-01"" />
                    <!-- [C-CDA PREG] Pregnancy Related Finding -->
                    <templateId root=""2.16.840.1.113883.10.20.22.4.298""
                        extension=""2018-04-01"" />
                    <id root=""9f96453f-8a20-4673-a255-2140db3a679a"" />
                    <code code=""404684003""
                        displayName=""Finding""
                        codeSystem=""2.16.840.1.113883.6.96""
                        codeSystemName=""SNOMED CT"">
                        <translation code=""75321-0""
                            codeSystem=""2.16.840.1.113883.6.1""
                            codeSystemName=""LOINC""
                            displayName=""Clinical finding"" />
                    </code>
                    <statusCode code=""completed"" />
                    <effectiveTime>
                        <low value=""20180101"" />
                    </effectiveTime>
                    <value xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""CD"" code=""6096002""
                        codeSystem=""2.16.840.1.113883.6.96""
                        codeSystemName=""SNOMED CT""
                        displayName=""Breech presentation (finding)"" />
                </observation>";

            var parser = new CcdaDataParser();
            var parsedXml = parser.Parse(xmlString) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "templateId", "2.16.840.1.113883.10.20.22.4.298" },
                { "observationEntry", parsedXml["observation"] },
            };

            var actualFhir = GetFhirObjectFromTemplate<Observation>(ECRPath, attributes);

            Assert.Equal("Observation", actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);

            Assert.NotEmpty(actualFhir.Identifier);
            Assert.Equal("Final", actualFhir.Status.ToString());

            Assert.Equal("http://snomed.info/sct", actualFhir.Code.Coding[0].System);
            Assert.Equal("404684003", actualFhir.Code.Coding[0].Code);
            Assert.Equal("Clinical finding", actualFhir.Code.Coding[0].Display);

            Assert.Equal("http://loinc.org", actualFhir.Code.Coding[1].System);
            Assert.Equal("75321-0", actualFhir.Code.Coding[1].Code);
            Assert.Equal("Clinical finding", actualFhir.Code.Coding[1].Display);

            Assert.Equal("2018-01-01", (actualFhir.Effective as Period)?.Start);

            Assert.IsType<CodeableConcept>(actualFhir.Value);
            var value = (CodeableConcept)actualFhir.Value;

            Assert.Equal("6096002", value.Coding.First().Code);
            Assert.Equal("http://snomed.info/sct", value.Coding.First().System);
            Assert.Equal("Breech presentation", value.Coding.First().Display);
        }
    }
}
