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
    public class ObservationPregnancyStatusTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "Resource",
            "ObservationPregnancyStatus.liquid"
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
            var parsedXml = parser.Parse(xmlString) as Dictionary<string, object>;

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
                "Delivery date Estimated from ovulation date",
                eddComponent.Code.Coding.First().Display
            );
            ////// Estimated gestational age (days)
            Assert.Equal("2017-05-22", eddComponent.Value.ToString());
        }

        [Fact]
        public void PregnancyStatus_EDDEntryRelationship() 
        {
          var xmlString = @"
          <observation classCode=""OBS"" moodCode=""EVN"">
							<templateId root=""2.16.840.1.113883.10.20.15.3.8""/>
							<id extension=""Z4645605^67605^77386006"" root=""1.2.840.114350.1.13.478.2.7.1.1040.6""/>
							<code code=""ASSERTION"" codeSystem=""2.16.840.1.113883.5.4""/>
							<statusCode code=""completed""/>
							<effectiveTime>
								<low value=""20251124""/>
							</effectiveTime>
							<value xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" code=""77386006"" codeSystem=""2.16.840.1.113883.6.96"" codeSystemName=""SNOMED CT"" displayName=""Pregnancy"" xsi:type=""CD""/>
							<entryRelationship typeCode=""REFR"">
								<observation classCode=""OBS"" moodCode=""EVN"">
									<templateId root=""2.16.840.1.113883.10.20.15.3.1""/>
									<code code=""11778-8"" codeSystem=""2.16.840.1.113883.6.1"" codeSystemName=""LOINC"" displayName=""Estimated date of delivery""/>
									<statusCode code=""completed""/>
									<value xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" value=""20260817"" xsi:type=""TS""/>
								</observation>
							</entryRelationship>
						</observation>";

          var parser = new CcdaDataParser();
          var parsedXml = parser.Parse(xmlString) as Dictionary<string, object>;

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

          Assert.Equal("2025-11-24", (actualFhir.Effective as Period).Start);

          Assert.Equal("77386006", (actualFhir.Value as CodeableConcept).Coding.First().Code);
          Assert.Equal(
              "http://snomed.info/sct",
              (actualFhir.Value as CodeableConcept).Coding.First().System
          );

          // Components
          //// Estimated Date of Delivery (EDD)
          var eddComponent = actualFhir.Component.Find(c =>
              c.Value is Hl7.Fhir.Model.FhirDateTime
          );
          Assert.Equal("2026-08-17", eddComponent.Value.ToString());
          Assert.Equal("11778-8", eddComponent.Code.Coding.First().Code);
          Assert.Equal("http://loinc.org", eddComponent.Code.Coding.First().System);
          Assert.Equal(
              "Delivery date Estimated",
              eddComponent.Code.Coding.First().Display
          );
        }

        [Fact]
        public void PregnancyStatus_CommentActivity()
        {
          var xmlString = @"
          <observation classCode=""OBS"" moodCode=""EVN"">
							<templateId root=""2.16.840.1.113883.10.20.15.3.8""/>
							<templateId extension=""2018-04-01"" root=""2.16.840.1.113883.10.20.22.4.293""/>
							<id extension=""138914829"" root=""1.2.840.114350.1.13.363.2.7.9.728366.79666980""/>
							<code code=""ASSERTION"" codeSystem=""2.16.840.1.113883.5.4""/>
							<statusCode code=""completed""/>
							<effectiveTime>
								<low value=""20250727""/>
							</effectiveTime>
							<value xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" code=""77386006"" codeSystem=""2.16.840.1.113883.6.96"" codeSystemName=""SNOMED CT"" displayName=""Pregnant"" xsi:type=""CD""/>
							<entryRelationship typeCode=""REFR"">
								<observation classCode=""OBS"" moodCode=""EVN"">
									<templateId extension=""2018-04-01"" root=""2.16.840.1.113883.10.20.22.4.297""/>
									<id extension=""138914829-2-1"" root=""1.2.840.114350.1.13.363.2.7.9.728366.79666980""/>
									<code code=""11779-6"" codeSystem=""2.16.840.1.113883.6.1"" codeSystemName=""LOINC"" displayName=""Delivery date Estimated from last menstrual period""/>
									<statusCode code=""completed""/>
									<effectiveTime value=""20251030""/>
									<value xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" value=""20260427"" xsi:type=""TS""/>
									<entryRelationship typeCode=""REFR"">
										<act classCode=""ACT"" moodCode=""EVN"">
											<templateId root=""2.16.840.1.113883.10.20.22.4.64""/>
											<code code=""48767-8"" codeSystem=""2.16.840.1.113883.6.1"" codeSystemName=""LOINC"" displayName=""Annotation comment""/>
											<text>
												<reference value=""#datingevent26comment""/>
											</text>
										</act>
									</entryRelationship>
								</observation>
							</entryRelationship>
							<entryRelationship typeCode=""REFR"">
								<observation classCode=""OBS"" moodCode=""EVN"">
									<templateId extension=""2018-04-01"" root=""2.16.840.1.113883.10.20.22.4.297""/>
									<id extension=""138914829-5-2"" root=""1.2.840.114350.1.13.363.2.7.9.728366.79666980""/>
									<code code=""11781-2"" codeSystem=""2.16.840.1.113883.6.1"" codeSystemName=""LOINC"" displayName=""Delivery date US composite estimate""/>
									<statusCode code=""completed""/>
									<effectiveTime value=""20251104""/>
									<value xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" value=""20260503"" xsi:type=""TS""/>
									<entryRelationship typeCode=""REFR"">
										<act classCode=""ACT"" moodCode=""EVN"">
											<templateId root=""2.16.840.1.113883.10.20.22.4.64""/>
											<code code=""48767-8"" codeSystem=""2.16.840.1.113883.6.1"" codeSystemName=""LOINC"" displayName=""Annotation comment""/>
											<text>
												<reference value=""#datingevent27comment""/>
											</text>
										</act>
									</entryRelationship>
								</observation>
							</entryRelationship>
						</observation>";

          var parser = new CcdaDataParser();
          var parsedXml = parser.Parse(xmlString) as Dictionary<string, object>;
          var text = new Dictionary<string, object>
          {
              {
                  "_innerText",
                  @"<content ID=""datingevent26comment"">LMP comment</content>
                    <content ID=""datingevent27comment"">Ultrasound comment</content>"
              },
          };

          var attributes = new Dictionary<string, object>
          {
              { "ID", "1234" },
              { "patientId", "urn:uuid:9876" },
              { "observationCategory", "exam" },
              { "observationEntry", parsedXml["observation"] },
              { "text", text },
          };

          var actualFhir = GetFhirObjectFromTemplate<Observation>(ECRPath, attributes);

          Assert.Equal("Observation", actualFhir.TypeName);
          Assert.Equal("1234", actualFhir.Id);
          Assert.Equal(
              "http://hl7.org/fhir/us/ecr/StructureDefinition/us-ph-pregnancy-status-observation",
              Assert.Single(actualFhir.Meta.Profile)
          );

          var identifier = Assert.Single(actualFhir.Identifier);
          Assert.Equal(
              "urn:oid:1.2.840.114350.1.13.363.2.7.9.728366.79666980",
              identifier.System
          );
          Assert.Equal("138914829", identifier.Value);

          var category = Assert.Single(Assert.Single(actualFhir.Category).Coding);
          Assert.Equal("http://terminology.hl7.org/CodeSystem/observation-category", category.System);
          Assert.Equal("exam", category.Code);

          Assert.Equal(ObservationStatus.Final, actualFhir.Status);

          var observationCode = Assert.Single(actualFhir.Code.Coding);
          Assert.Equal("http://loinc.org", observationCode.System);
          Assert.Equal("82810-3", observationCode.Code);
          Assert.Equal("Pregnancy status", observationCode.Display);

          var effectivePeriod = Assert.IsType<Period>(actualFhir.Effective);
          Assert.Equal("2025-07-27", effectivePeriod.Start);

          var pregnancyStatus = Assert.Single(
              Assert.IsType<CodeableConcept>(actualFhir.Value).Coding
          );
          Assert.Equal("http://snomed.info/sct", pregnancyStatus.System);
          Assert.Equal("77386006", pregnancyStatus.Code);
          Assert.Equal("Pregnancy", pregnancyStatus.Display);

          Assert.Collection(
              actualFhir.Note,
              note => Assert.Equal("LMP comment", note.Text),
              note => Assert.Equal("Ultrasound comment", note.Text)
          );

          const string determinedExtensionUrl =
              "http://hl7.org/fhir/us/ecr/StructureDefinition/us-ph-date-determined-extension";

          Assert.Collection(
              actualFhir.Component,
              component =>
              {
                  var code = Assert.Single(component.Code.Coding);
                  Assert.Equal("http://loinc.org", code.System);
                  Assert.Equal("11779-6", code.Code);
                  Assert.Equal("Delivery date Estimated from last menstrual period", code.Display);
                  Assert.Equal("2026-04-27", Assert.IsType<FhirDateTime>(component.Value).Value);

                  var extension = Assert.Single(component.Extension);
                  Assert.Equal(determinedExtensionUrl, extension.Url);
                  Assert.Equal("2025-10-30", Assert.IsType<FhirDateTime>(extension.Value).Value);
              },
              component =>
              {
                  var code = Assert.Single(component.Code.Coding);
                  Assert.Equal("http://loinc.org", code.System);
                  Assert.Equal("11781-2", code.Code);
                  Assert.Equal("Delivery date US composite estimate", code.Display);
                  Assert.Equal("2026-05-03", Assert.IsType<FhirDateTime>(component.Value).Value);

                  var extension = Assert.Single(component.Extension);
                  Assert.Equal(determinedExtensionUrl, extension.Url);
                  Assert.Equal("2025-11-04", Assert.IsType<FhirDateTime>(extension.Value).Value);
              }
          );
        }
    }
}
