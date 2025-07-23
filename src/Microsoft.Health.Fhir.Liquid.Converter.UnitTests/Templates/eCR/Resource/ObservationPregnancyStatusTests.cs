using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotLiquid;
using Hl7.Fhir.ElementModel.Types;
using Hl7.Fhir.Model;
using Microsoft.Health.Fhir.Liquid.Converter.Parsers;
using Namotion.Reflection;
using Xunit;

namespace Microsoft.Health.Fhir.Liquid.Converter.UnitTests
{
    public class ObservationPregnancyStatusTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "Resource",
            "_ObservationPregnancyStatus.liquid"
        );

        [Fact]
        public void PregnancyStatus_AllFields()
        {
            var xmlString =
                @"
<observation
    classCode=""OBS""
    moodCode=""EVN""
    xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
    xsi:schemaLocation=""urn:hl7-org:v3 ../../../cda-core-2.0/schema/extensions/SDTC/infrastructure/cda/CDA_SDTC.xsd""
    xmlns=""urn:hl7-org:v3""
    xmlns:cda=""urn:hl7-org:v3""
    xmlns:sdtc=""urn:hl7-org:sdtc""
    xmlns:voc=""http://www.lantanagroup.com/voc"">
  <templateId root=""2.16.840.1.113883.10.20.15.3.8"" />
  <templateId root=""2.16.840.1.113883.10.20.22.4.293"" extension=""2020-04-01"" />
  <id root=""bab77407-f76d-4a51-a2ed-980c8a59fe28"" />
  <code code=""ASSERTION"" codeSystem=""2.16.840.1.113883.5.4"" />
  <statusCode code=""completed"" />
  <effectiveTime>
    <low value=""20170826"" />
  </effectiveTime>
  <value xsi:type=""CD"" code=""77386006"" displayName=""Pregnant"" codeSystem=""2.16.840.1.113883.6.96""
    codeSystemName=""SNOMED CT"" />
  <methodCode code=""16310003"" displayName=""Diagnostic ultrasonography (procedure)""
    codeSystem=""2.16.840.1.113883.6.96"" codeSystemName=""SNOMED CT"" />
  <performer>
    <time value=""20171001"" />
    <assignedEntity>
      <id nullFlavor=""NA"" />
    </assignedEntity>
  </performer>
  <author>
    <time value=""201710011035"" />
    <assignedAuthor>
      <id nullFlavor=""NA"" />
    </assignedAuthor>
  </author>
  <entryRelationship typeCode=""REFR"">
    <observation classCode=""OBS"" moodCode=""EVN"">
      <templateId root=""2.16.840.1.113883.10.20.22.4.297"" extension=""2020-04-01"" />
      <id root=""f3dfc576-2329-4f71-af3f-d500cab1146b"" />
      <code code=""11780-4"" codeSystem=""2.16.840.1.113883.6.1""
        displayName=""Delivery date estimated from ovulation date"" codeSystemName=""LOINC"" />
      <statusCode code=""completed"" />
      <effectiveTime value=""201710011015"" />
      <value xsi:type=""TS"" value=""20170522"" />
    </observation>
  </entryRelationship>
  <entryRelationship typeCode=""REFR"">
    <observation classCode=""OBS"" moodCode=""EVN"">
      <templateId root=""2.16.840.1.113883.10.20.22.4.280"" extension=""2020-04-01"" />
      <id root=""9ae62b42-0e67-4ddc-8ef7-909d03732292"" />
      <code code=""11887-7"" codeSystem=""2.16.840.1.113883.6.1""
        displayName=""Gestational age Estimated from selected delivery date"" codeSystemName=""LOINC"" />
      <statusCode code=""completed"" />
      <effectiveTime value=""201710011015"" />
      <value xsi:type=""PQ"" unit=""d"" value=""143"" />
      <entryRelationship typeCode=""REFR"">
        <act classCode=""ACT"" moodCode=""EVN"">
          <templateId root=""2.16.840.1.113883.10.20.22.4.122"" />
          <id root=""f3dfc576-2329-4f71-af3f-d500cab1146b"" />
          <code nullFlavor=""NP"" />
          <statusCode code=""completed"" />
        </act>
      </entryRelationship>
    </observation>
  </entryRelationship>
</observation>";

            var parser = new CcdaDataParser();
            var parsedXml = parser.Parse(xmlString) as Dictionary<string, object>; ;

            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "patientId", "urn:uuid:9876" },
                { "observationCategory", "exam" },
                { "observationEntry", parsedXml["observation"] },
            };

            var actualFhir = GetFhirObjectFromTemplate<Observation>(ECRPath, attributes);

            Assert.Equal("Observation", actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);
            Assert.Equal(
                "http://hl7.org/fhir/us/ecr/StructureDefinition/us-ph-pregnancy-status-observation",
                actualFhir.Meta.Profile.First()
            );
            Assert.NotEmpty(actualFhir.Identifier);
            Assert.Equal("Final", actualFhir.Status.ToString());
            Assert.Equal("http://loinc.org", actualFhir.Code.Coding.First().System);
            Assert.Equal("82810-3", actualFhir.Code.Coding.First().Code);
            Assert.Equal("urn:uuid:9876", actualFhir.Subject.Reference);
            Assert.Equal("2017-08-26", (actualFhir.Effective as Period).Start);
            Assert.Equal("77386006", (actualFhir.Value as CodeableConcept).Coding.First().Code);
            Assert.Equal(
                "http://snomed.info/sct",
                (actualFhir.Value as CodeableConcept).Coding.First().System
            );
            Assert.Equal("16310003", (actualFhir.Method as CodeableConcept).Coding.First().Code);
            Assert.Equal(
                "http://snomed.info/sct",
                (actualFhir.Method as CodeableConcept).Coding.First().System
            );

            // Components
            var determinedExtensionUrl =
                "http://hl7.org/fhir/us/ecr/StructureDefinition/us-ph-date-determined-extension";

            //// Estimated gestational age of pregnancy component
            var EstimatedGestationalAgeComponent = actualFhir.Component.Find(c =>
                c.Value is Hl7.Fhir.Model.Quantity
            );
            Assert.Equal(
                determinedExtensionUrl,
                EstimatedGestationalAgeComponent.Extension.First().Url
            );
            ////// Estimated gestational age of pregnancy determination date.
            Assert.Equal(
                "2017-10-01T10:15:00",
                EstimatedGestationalAgeComponent.Extension.First().Value.ToString()
            );
            ////// Estimated Gestational Age Code Including Method
            Assert.Equal("11887-7", EstimatedGestationalAgeComponent.Code.Coding.First().Code);
            Assert.Equal(
                "http://loinc.org",
                EstimatedGestationalAgeComponent.Code.Coding.First().System
            );
            Assert.Equal(
                "Gestational age Estimated from selected delivery date",
                EstimatedGestationalAgeComponent.Code.Coding.First().Display
            );
            ////// Estimated gestational age (days)
            var estimatedGestationalAge =
                EstimatedGestationalAgeComponent.Value as Hl7.Fhir.Model.Quantity;
            Assert.Equal(143, estimatedGestationalAge.Value);
            Assert.Equal("d", estimatedGestationalAge.Unit);

            //// Estimated Date of Delivery (EDD)
            var eddComponent = actualFhir.Component.Find(c =>
                c.Value is Hl7.Fhir.Model.FhirDateTime
            );

            Assert.Equal(determinedExtensionUrl, eddComponent.Extension.First().Url);
            ////// Estimated gestational age of pregnancy determination date.
            Assert.Equal("2017-10-01T10:15:00", eddComponent.Extension.First().Value.ToString());
            ////// Estimated Gestational Age Code Including Method
            Assert.Equal("11780-4", eddComponent.Code.Coding.First().Code);
            Assert.Equal("http://loinc.org", eddComponent.Code.Coding.First().System);
            Assert.Equal(
                "Delivery date estimated from ovulation date",
                eddComponent.Code.Coding.First().Display
            );
            ////// Estimated gestational age (days)
            Assert.Equal("2017-05-22", eddComponent.Value.ToString());
        }
    }
}
