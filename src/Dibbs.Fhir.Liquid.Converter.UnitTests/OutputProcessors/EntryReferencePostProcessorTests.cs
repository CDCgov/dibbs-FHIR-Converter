// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Linq;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Dibbs.Fhir.Liquid.Converter.OutputProcessors;
using Xunit;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests.OutputProcessors
{
    public class EntryReferencePostProcessorTests
    {
        private const string EntryReferenceUrl = "https://github.com/CDCgov/dibbs-FHIR-Converter/StructureDefinition/cda-entry-reference";
        private const string ActRelationshipCodeSystem = "http://terminology.hl7.org/CodeSystem/v3-ActRelationshipType";

        [Theory]
        [InlineData("<ClinicalDocument />")]
        [InlineData("<ClinicalDocument><text>2.16.840.1.113883.10.20.22.4.122</text></ClinicalDocument>")]
        public void Process_DoesNotParseFhirJson_WhenNoEntryReferenceActExists(string cdaData)
        {
            const string bundleJson = "{\"resourceType\":\"Bundle\",\"entry\":[]}";

            var result = EntryReferencePostProcessor.Process(bundleJson, cdaData);

            Assert.Equal(bundleJson, result);
        }

        [Fact]
        public void Process_PreservesFhirJson_WhenEntryReferenceCannotBeResolved()
        {
            const string bundleJson = "{\"resourceType\":\"Bundle\",\"entry\":[]}";
            const string cdaData = "<ClinicalDocument><act><templateId root=\"2.16.840.1.113883.10.20.22.4.122\"/></act></ClinicalDocument>";

            var result = EntryReferencePostProcessor.Process(bundleJson, cdaData);

            Assert.Equal(bundleJson, result);
        }

        [Fact]
        public void Process_AddsExtensionFromSourceToTargetResource()
        {
            var cdaDocument = XDocument.Parse(@"<ClinicalDocument xmlns=""urn:hl7-org:v3"">
              <component><section>
                <templateId root=""2.16.840.1.113883.10.20.22.2.3.1""/>
                <entry>
                  <organizer>
                    <id root=""1.2.840.114350.1.13.4304.2.7.2.798268"" extension=""XX97036""/>
                    <component>
                      <procedure>
                        <entryRelationship typeCode=""COMP"" inversionInd=""true"">
                          <act>
                            <templateId root=""2.16.840.1.113883.10.20.22.4.122""/>
                            <id root=""1.2.840.114350.1.13.4304.2.7.1.1988.1"" extension=""XX28196-5493""/>
                            <code nullFlavor=""NP""/>
                          </act>
                        </entryRelationship>
                      </procedure>
                    </component>
                  </organizer>
                </entry>
                <entry>
                  <procedure>
                    <id root=""1.2.840.114350.1.13.4304.2.7.1.1988.1"" extension=""XX28196-5493""/>
                    <code code=""58410-2""/>
                  </procedure>
                </entry>
              </section></component>
            </ClinicalDocument>");

            var bundle = ParseBundle(@"{
              ""resourceType"": ""Bundle"",
              ""entry"": [
                {
                  ""resource"": {
                    ""resourceType"": ""DiagnosticReport"",
                    ""id"": ""report-1"",
                    ""identifier"": [
                      {
                        ""system"": ""urn:oid:1.2.840.114350.1.13.4304.2.7.2.798268"",
                        ""value"": ""XX97036""
                      }
                    ]
                  }
                },
                {
                  ""resource"": {
                    ""resourceType"": ""Procedure"",
                    ""id"": ""proc-1"",
                    ""identifier"": [
                      {
                        ""system"": ""urn:oid:1.2.840.114350.1.13.4304.2.7.1.1988.1"",
                        ""value"": ""XX28196-5493""
                      }
                    ]
                  }
                }
              ]
            }");

            var updated = EntryReferencePostProcessor.Process(bundle, cdaDocument);
            var report = updated["entry"]?[0]?["resource"] as JsonObject;
            var extensions = report?["extension"] as JsonArray;

            Assert.NotNull(extensions);
            var extension = Assert.Single(extensions);
            Assert.Equal(EntryReferenceUrl, extension?["url"]?.GetValue<string>());

            var extensionParts = extension?["extension"] as JsonArray;
            Assert.NotNull(extensionParts);

            var targetExtension = extensionParts.FirstOrDefault(part => part?["url"]?.GetValue<string>() == "target");
            Assert.NotNull(targetExtension);
            Assert.Equal("Procedure/proc-1", targetExtension?["valueReference"]?["reference"]?.GetValue<string>());

            var typeCodeExtension = extensionParts.FirstOrDefault(part => part?["url"]?.GetValue<string>() == "typeCode");
            Assert.NotNull(typeCodeExtension);
            Assert.Equal("COMP", typeCodeExtension?["valueCoding"]?["code"]?.GetValue<string>());
            Assert.Equal(ActRelationshipCodeSystem, typeCodeExtension?["valueCoding"]?["system"]?.GetValue<string>());

            var inversionExtension = extensionParts.FirstOrDefault(part => part?["url"]?.GetValue<string>() == "inversionInd");
            Assert.NotNull(inversionExtension);
            Assert.True(inversionExtension?["valueBoolean"]?.GetValue<bool>());
        }

        [Fact]
        public void Process_SkipsWhenTargetNotFoundInBundle()
        {
            var cdaDocument = XDocument.Parse(@"<ClinicalDocument xmlns=""urn:hl7-org:v3"">
              <entry>
                <observation>
                  <id root=""source-obs""/>
                  <entryRelationship typeCode=""REFR"">
                    <act>
                      <templateId root=""2.16.840.1.113883.10.20.22.4.122""/>
                      <id root=""missing-target""/>
                    </act>
                  </entryRelationship>
                </observation>
              </entry>
            </ClinicalDocument>");

            var bundle = ParseBundle(@"{
              ""resourceType"": ""Bundle"",
              ""entry"": [
                {
                  ""resource"": {
                    ""resourceType"": ""Observation"",
                    ""id"": ""obs-1"",
                    ""identifier"": [ { ""value"": ""source-obs"" } ]
                  }
                }
              ]
            }");

            var updated = EntryReferencePostProcessor.Process(bundle, cdaDocument);
            var observation = updated["entry"]?[0]?["resource"] as JsonObject;

            Assert.Null(observation?["extension"]);
        }

        [Fact]
        public void Process_SkipsWhenSourceNotFoundInBundle()
        {
            var cdaDocument = XDocument.Parse(@"<ClinicalDocument xmlns=""urn:hl7-org:v3"">
              <entry>
                <act>
                  <id root=""unconverted-act""/>
                  <entryRelationship typeCode=""REFR"">
                    <act>
                      <templateId root=""2.16.840.1.113883.10.20.22.4.122""/>
                      <id root=""target-proc""/>
                    </act>
                  </entryRelationship>
                </act>
              </entry>
              <entry>
                <procedure>
                  <id root=""target-proc""/>
                </procedure>
              </entry>
            </ClinicalDocument>");

            var bundle = ParseBundle(@"{
              ""resourceType"": ""Bundle"",
              ""entry"": [
                {
                  ""resource"": {
                    ""resourceType"": ""Procedure"",
                    ""id"": ""proc-1"",
                    ""identifier"": [ { ""value"": ""target-proc"" } ]
                  }
                }
              ]
            }");

            var updated = EntryReferencePostProcessor.Process(bundle, cdaDocument);
            var procedure = updated["entry"]?[0]?["resource"] as JsonObject;

            Assert.Null(procedure?["extension"]);
        }
        
        [Fact]
        public void Process_StopsAtSectionBoundary()
        {
            var cdaDocument = XDocument.Parse(@"<ClinicalDocument xmlns=""urn:hl7-org:v3"">
              <id root=""doc-header-id""/>
              <component><section>
                <id root=""unmatched-section-id""/>
                <entry>
                  <observation>
                    <entryRelationship typeCode=""REFR"">
                      <act>
                        <templateId root=""2.16.840.1.113883.10.20.22.4.122""/>
                        <id root=""target-proc-id""/>
                      </act>
                    </entryRelationship>
                  </observation>
                </entry>
              </section></component>
            </ClinicalDocument>");

            var bundle = ParseBundle(@"{
              ""resourceType"": ""Bundle"",
              ""entry"": [
                {
                  ""resource"": {
                    ""resourceType"": ""Composition"",
                    ""id"": ""comp-1"",
                    ""identifier"": [ { ""value"": ""doc-header-id"" } ]
                  }
                },
                {
                  ""resource"": {
                    ""resourceType"": ""Procedure"",
                    ""id"": ""proc-1"",
                    ""identifier"": [ { ""value"": ""target-proc-id"" } ]
                  }
                }
              ]
            }");

            var updated = EntryReferencePostProcessor.Process(bundle, cdaDocument);
            var composition = updated["entry"]?[0]?["resource"] as JsonObject;

            Assert.Null(composition?["extension"]);
        }

        private static JsonNode ParseBundle(string json)
        {
            return JsonNode.Parse(json) ?? new JsonObject();
        }
    }
}
