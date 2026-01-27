using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hl7.Fhir.Model;
using Xunit;
using Dibbs.Fhir.Liquid.Converter.DataParsers;
using System;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests
{
    public class ObservationRRInformationTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "Resource",
            "ObservationRRInformation.liquid"
        );

        [Fact]
        public void ObservationRRInformation_AllFields()
        {
            // from 3.1 eCR
            var xmlStr = @"
            <organizer classCode=""CLUSTER"" moodCode=""EVN""
                xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                xsi:schemaLocation=""urn:hl7-org:v3 ../../../cda-core-2.0/schema/extensions/SDTC/infrastructure/cda/CDA_SDTC.xsd""
                xmlns=""urn:hl7-org:v3""
                xmlns:cda=""urn:hl7-org:v3""
                xmlns:sdtc=""urn:hl7-org:sdtc""
                xmlns:voc=""http://www.lantanagroup.com/voc"">
                <templateId extension=""2017-04-01""
                    root=""2.16.840.1.113883.10.20.15.2.3.13"" />
                <id root=""fcf92143-4289-450e-9550-8d574facf626"" />
                <code code=""RRVS7""
                    codeSystem=""2.16.840.1.114222.4.5.274""
                    codeSystemName=""PHIN VS (CDC Local Coding System)""
                    displayName=""Both patient home address and provider facility address""></code>
                <statusCode code=""completed"" />
                <component typeCode=""COMP"">
                    <observation classCode=""OBS"" moodCode=""EVN"">
                        <templateId extension=""2017-04-01""
                            root=""2.16.840.1.113883.10.20.15.2.3.19"" />
                        <id root=""e39d6ae2-8c6e-4638-9b33-412996586f41"" />
                        <code code=""RR1""
                            codeSystem=""2.16.840.1.114222.4.5.232""
                            codeSystemName=""PHIN Questions""
                            displayName=""Determination of reportability"" />
                        <value code=""RRVS1""
                            codeSystem=""2.16.840.1.114222.4.5.274""
                            codeSystemName=""PHIN VS (CDC Local Coding System)""
                            displayName=""Reportable"" xsi:type=""CD"" />
                        <entryRelationship typeCode=""RSON"">
                            <observation classCode=""OBS"" moodCode=""EVN"">
                                <templateId extension=""2017-04-01""
                                    root=""2.16.840.1.113883.10.20.15.2.3.26"" />
                                <id
                                    root=""8709a342-56ad-425a-b7b1-76a16c2dd2d5"" />
                                <code code=""RR2""
                                    codeSystem=""2.16.840.1.114222.4.5.232""
                                    codeSystemName=""PHIN Questions""
                                    displayName=""Determination of reportability reason"" />
                                <value xsi:type=""ST"">Reason for
                                    determination of reportability</value>
                            </observation>
                        </entryRelationship>
                        <entryRelationship typeCode=""RSON"">
                            <observation classCode=""OBS"" moodCode=""EVN"">
                                <templateId extension=""2017-04-01""
                                    root=""2.16.840.1.113883.10.20.15.2.3.27"" />
                                <id
                                    root=""f2dfdffb-bccb-4ee4-9b6c-0ae82b15ada6"" />
                                <code code=""RR3""
                                    codeSystem=""2.16.840.1.114222.4.5.232""
                                    codeSystemName=""PHIN Questions""
                                    displayName=""Determination of reportability rule"" />
                                <value xsi:type=""ST"">Rule used in
                                    reportability determination</value>
                            </observation>
                        </entryRelationship>
                    </observation>
                </component>
                <component typeCode=""COMP"">
                    <observation classCode=""OBS"" moodCode=""EVN"">
                        <templateId extension=""2017-04-01""
                            root=""2.16.840.1.113883.10.20.15.2.3.14"" />
                        <id root=""8334d0bd-a404-4e12-9e9c-e278669e59ba"" />
                        <code code=""RR4""
                            codeSystem=""2.16.840.1.114222.4.5.232""
                            codeSystemName=""PHIN Questions""
                            displayName=""Timeframe to report (urgency)"" />
                        <value unit=""h"" value=""24"" xsi:type=""PQ"" />
                    </observation>
                </component>
            </organizer>
            ";

            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;
            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "observationEntry", parsed["organizer"]},
            };
            var actualFhir = GetFhirObjectFromTemplate<Observation>(ECRPath, attributes);

            Assert.Equal(ResourceType.Observation.ToString(), actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);
            Assert.Equal(ObservationStatus.Final, actualFhir.Status);
            Assert.Equal(
                "http://hl7.org/fhir/us/ecr/StructureDefinition/rr-reportability-information-observation",
                actualFhir.Meta.Profile.First()
            );

            Assert.NotNull(actualFhir.Code);
            Assert.Equal("urn:oid:2.16.840.1.114222.4.5.274", actualFhir.Code?.Coding?.First().System);
            Assert.Equal("Both patient home address and provider facility address", actualFhir.Code?.Coding?.First().Display);
            Assert.Equal("RRVS7", actualFhir.Code?.Coding?.First().Code);

            Assert.IsType<CodeableConcept>(actualFhir.Component.First().Code);
            var timeframeToReport = (CodeableConcept)actualFhir.Component.First().Code;
            Assert.Equal("RR4", timeframeToReport.Coding.First().Code);
            Assert.Equal("urn:oid:2.16.840.1.114222.4.5.232", timeframeToReport.Coding.First().System);
            Assert.Equal("Timeframe to report (urgency)", timeframeToReport.Coding.First().Display);

            Assert.Equal(24, (actualFhir.Component.First().Value as Quantity)?.Value);
            Assert.Equal("h", (actualFhir.Component.First().Value as Quantity)?.Unit);

            Assert.Equal("Reportable", actualFhir.GetExtensionValue<CodeableConcept>("http://hl7.org/fhir/us/ecr/StructureDefinition/us-ph-determination-of-reportability-extension").Coding.First().Display);
            Assert.Equal("RRVS1", actualFhir.GetExtensionValue<CodeableConcept>("http://hl7.org/fhir/us/ecr/StructureDefinition/us-ph-determination-of-reportability-extension").Coding.First().Code);
            var extRRReason = actualFhir.GetExtension("http://hl7.org/fhir/us/ecr/StructureDefinition/us-ph-determination-of-reportability-reason-extension");
            Assert.Equal("Reason for determination of reportability", ((FhirString)extRRReason.Value).Value);
            var extRRRule = actualFhir.GetExtension("http://hl7.org/fhir/us/ecr/StructureDefinition/us-ph-determination-of-reportability-rule-extension");
            Assert.Equal("Rule used in reportability determination", ((FhirString)extRRRule.Value).Value);
        }
    }
}