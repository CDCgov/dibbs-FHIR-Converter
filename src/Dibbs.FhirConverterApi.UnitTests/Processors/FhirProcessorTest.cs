using System.Net;
using System.Text.Json.Nodes;
using Dibbs.FhirConverterApi.Processors;

namespace Dibbs.FhirConverterApi.UnitTests.Processors;

public class FhirProcessorTest
{
  private const string EicrXml = """
    <Bundle xmlns="http://hl7.org/fhir">
      <id value="eicr-bundle" />
      <meta>
        <profile value="http://hl7.org/fhir/us/ecr/StructureDefinition/eicr-document-bundle" />
      </meta>
      <type value="document" />
      <entry>
        <fullUrl value="urn:uuid:11111111-1111-1111-1111-111111111111" />
        <resource>
          <Composition>
            <meta>
              <profile value="http://hl7.org/fhir/us/ecr/StructureDefinition/eicr-composition" />
            </meta>
            <status value="final" />
            <type>
              <coding>
                <system value="http://loinc.org" />
                <code value="55751-2" />
              </coding>
            </type>
            <subject>
              <reference value="urn:uuid:11111111-1111-1111-1111-111111111112" />
            </subject>
            <date value="2026-09-23T12:00:00Z" />
            <title value="Initial Public Health Case Report" />
          </Composition>
        </resource>
      </entry>
      <entry>
        <fullUrl value="urn:uuid:11111111-1111-1111-1111-111111111112" />
        <resource>
          <Patient>
            <birthDate value="2000-01-01" />
          </Patient>
        </resource>
      </entry>
      <entry>
        <fullUrl value="urn:uuid:11111111-1111-1111-1111-111111111113" />
        <resource>
          <Observation>
            <status value="final" />
            <code>
              <text value="eICR observation" />
            </code>
            <subject>
              <reference value="urn:uuid:11111111-1111-1111-1111-111111111112" />
            </subject>
          </Observation>
        </resource>
      </entry>
    </Bundle>
    """;

  private const string RrXml = """
    <Bundle xmlns="http://hl7.org/fhir">
      <id value="rr-bundle" />
      <meta>
        <profile value="http://hl7.org/fhir/us/ecr/StructureDefinition/rr-document-bundle" />
      </meta>
      <type value="document" />
      <entry>
        <fullUrl value="urn:uuid:22222222-2222-2222-2222-222222222221" />
        <resource>
          <Composition>
            <meta>
              <profile value="http://hl7.org/fhir/us/ecr/StructureDefinition/rr-composition" />
            </meta>
            <status value="final" />
            <type>
              <coding>
                <system value="http://loinc.org" />
                <code value="88085-6" />
              </coding>
            </type>
            <date value="2026-09-23T12:01:00Z" />
            <title value="Reportability Response" />
            <section>
              <extension url="http://hl7.org/fhir/us/ecr/StructureDefinition/rr-eicr-processing-status-extension">
                <extension url="eICRProcessingStatus">
                  <valueReference>
                    <reference value="urn:uuid:22222222-2222-2222-2222-222222222223" />
                  </valueReference>
                </extension>
              </extension>
              <code>
                <coding>
                  <system value="http://loinc.org" />
                  <code value="88082-3" />
                </coding>
              </code>
            </section>
          </Composition>
        </resource>
      </entry>
      <entry>
        <fullUrl value="urn:uuid:22222222-2222-2222-2222-222222222222" />
        <resource>
          <Patient>
            <birthDate value="1900-01-01" />
          </Patient>
        </resource>
      </entry>
      <entry>
        <fullUrl value="urn:uuid:22222222-2222-2222-2222-222222222223" />
        <resource>
          <Observation>
            <meta>
              <profile value="http://hl7.org/fhir/us/ecr/StructureDefinition/rr-eicr-processing-status-observation" />
            </meta>
            <status value="final" />
            <code>
              <coding>
                <code value="RRVS19" />
                <display value="eICR processed" />
              </coding>
            </code>
          </Observation>
        </resource>
      </entry>
      <entry>
        <fullUrl value="urn:uuid:22222222-2222-2222-2222-222222222224" />
        <resource>
          <Observation>
            <meta>
              <profile value="http://hl7.org/fhir/us/ecr/StructureDefinition/rr-relevant-reportable-condition-observation" />
            </meta>
            <status value="final" />
            <code>
              <coding>
                <system value="http://snomed.info/sct" />
                <code value="64572001" />
              </coding>
            </code>
            <subject>
              <reference value="urn:uuid:22222222-2222-2222-2222-222222222222" />
            </subject>
            <valueCodeableConcept>
              <coding>
                <system value="http://snomed.info/sct" />
                <code value="40468003" />
                <display value="Viral hepatitis, type A" />
              </coding>
            </valueCodeableConcept>
            <hasMember>
              <reference value="urn:uuid:22222222-2222-2222-2222-222222222225" />
            </hasMember>
          </Observation>
        </resource>
      </entry>
      <entry>
        <fullUrl value="urn:uuid:22222222-2222-2222-2222-222222222225" />
        <resource>
          <Observation>
            <meta>
              <profile value="http://hl7.org/fhir/us/ecr/StructureDefinition/rr-reportability-information-observation" />
            </meta>
            <extension url="http://hl7.org/fhir/us/ecr/StructureDefinition/rr-external-resource-extension">
              <valueReference>
                <reference value="urn:uuid:22222222-2222-2222-2222-222222222226" />
              </valueReference>
            </extension>
            <status value="final" />
            <code>
              <coding>
                <code value="RRVS5" />
              </coding>
            </code>
            <subject>
              <reference value="urn:uuid:22222222-2222-2222-2222-222222222222" />
            </subject>
            <performer>
              <reference value="urn:uuid:22222222-2222-2222-2222-222222222227" />
            </performer>
          </Observation>
        </resource>
      </entry>
      <entry>
        <fullUrl value="urn:uuid:22222222-2222-2222-2222-222222222226" />
        <resource>
          <DocumentReference>
            <meta>
              <profile value="http://hl7.org/fhir/us/ecr/StructureDefinition/rr-documentreference" />
            </meta>
            <status value="current" />
            <type>
              <text value="Reporting information" />
            </type>
            <subject>
              <reference value="urn:uuid:22222222-2222-2222-2222-222222222222" />
            </subject>
            <content>
              <attachment>
                <url value="https://example.org/reporting" />
              </attachment>
            </content>
          </DocumentReference>
        </resource>
      </entry>
      <entry>
        <fullUrl value="urn:uuid:22222222-2222-2222-2222-222222222227" />
        <resource>
          <Organization>
            <meta>
              <profile value="http://hl7.org/fhir/us/ecr/StructureDefinition/rr-rules-authoring-agency-organization" />
            </meta>
            <name value="Rules Agency" />
          </Organization>
        </resource>
      </entry>
      <entry>
        <fullUrl value="urn:uuid:22222222-2222-2222-2222-222222222228" />
        <resource>
          <Observation>
            <meta>
              <profile value="http://hl7.org/fhir/us/ecr/StructureDefinition/rr-relevant-reportable-condition-observation" />
            </meta>
            <status value="final" />
            <code>
              <coding>
                <code value="64572001" />
              </coding>
            </code>
            <subject>
              <reference value="urn:uuid:22222222-2222-2222-2222-222222222222" />
            </subject>
            <dataAbsentReason>
              <coding>
                <code value="not-applicable" />
              </coding>
            </dataAbsentReason>
            <hasMember>
              <reference value="urn:uuid:22222222-2222-2222-2222-222222222229" />
            </hasMember>
          </Observation>
        </resource>
      </entry>
      <entry>
        <fullUrl value="urn:uuid:22222222-2222-2222-2222-222222222229" />
        <resource>
          <Observation>
            <meta>
              <profile value="http://hl7.org/fhir/us/ecr/StructureDefinition/rr-reportability-information-observation" />
            </meta>
            <status value="final" />
            <code>
              <text value="No rule met" />
            </code>
            <performer>
              <reference value="urn:uuid:22222222-2222-2222-2222-222222222230" />
            </performer>
          </Observation>
        </resource>
      </entry>
      <entry>
        <fullUrl value="urn:uuid:22222222-2222-2222-2222-222222222230" />
        <resource>
          <Organization>
            <meta>
              <profile value="http://hl7.org/fhir/us/ecr/StructureDefinition/rr-routing-entity-organization" />
            </meta>
            <name value="Unused Routing Entity" />
          </Organization>
        </resource>
      </entry>
    </Bundle>
    """;

  [Fact]
  public void ConvertXmlToJson_ReturnsEquivalentFhirJson_WhenInputIsFhirXmlBundle()
  {
    const string fhirXml = """
      <Bundle xmlns="http://hl7.org/fhir">
        <id value="bundle-example" />
        <identifier>
          <system value="urn:ietf:rfc:3986" />
          <value value="urn:uuid:12345678-1234-1234-1234-123456789abc" />
        </identifier>
        <type value="collection" />
        <entry>
          <fullUrl value="urn:uuid:87654321-4321-4321-4321-cba987654321" />
          <resource>
            <Patient>
              <id value="patient-example" />
            </Patient>
          </resource>
        </entry>
      </Bundle>
      """;

    var actual = FhirProcessor.ConvertXmlToJson(fhirXml);
    var bundle = JsonNode.Parse(actual);

    Assert.Equal("Bundle", (string)bundle!["resourceType"] !);
    Assert.Equal("bundle-example", (string)bundle["id"] !);
    Assert.Equal(
      "urn:uuid:12345678-1234-1234-1234-123456789abc",
      (string)bundle["identifier"] !["value"] !);
    Assert.Equal("Patient", (string)bundle["entry"] ![0] !["resource"] !["resourceType"] !);
  }

  [Fact]
  public void ConvertXmlToJson_MergesSeparateFhirEicrAndRrForViewer()
  {
    var actual = JsonNode.Parse(FhirProcessor.ConvertXmlToJson(EicrXml, RrXml)) !;
    var entries = actual["entry"] !.AsArray();
    var resources = entries.Select(entry => entry!["resource"] !).ToList();

    Assert.Equal("eicr-bundle", (string)actual["id"] !);
    Assert.Equal("document", (string)actual["type"] !);
    Assert.Equal(8, entries.Count);
    Assert.Single(resources, resource => (string)resource["resourceType"] ! == "Composition");
    Assert.Single(resources, resource => (string)resource["resourceType"] ! == "Patient");
    Assert.DoesNotContain(resources, resource => (string)resource["resourceType"] ! == "Bundle");
    Assert.All(resources, resource => Assert.False(string.IsNullOrEmpty((string?)resource["id"])));

    var eicrPatient = Assert.Single(
      resources,
      resource => (string)resource["resourceType"] ! == "Patient");
    Assert.Equal("11111111-1111-1111-1111-111111111112", (string)eicrPatient["id"] !);
    Assert.Equal("2000-01-01", (string)eicrPatient["birthDate"] !);

    var eicrObservation = resources.Single(resource =>
      (string?)resource["id"] == "11111111-1111-1111-1111-111111111113");
    Assert.Equal(
      "Patient/11111111-1111-1111-1111-111111111112",
      (string)eicrObservation["subject"] !["reference"] !);

    var rrCondition = resources.Single(resource =>
      (string?)resource["id"] == "22222222-2222-2222-2222-222222222224");
    Assert.Equal(
      "Patient/11111111-1111-1111-1111-111111111112",
      (string)rrCondition["subject"] !["reference"] !);
    Assert.Equal(
      "Observation/22222222-2222-2222-2222-222222222225",
      (string)rrCondition["hasMember"] ![0] !["reference"] !);

    var rrInformation = resources.Single(resource =>
      (string?)resource["id"] == "22222222-2222-2222-2222-222222222225");
    Assert.Equal(
      "DocumentReference/22222222-2222-2222-2222-222222222226",
      (string)rrInformation["extension"] ![0] !["valueReference"] !["reference"] !);
    Assert.Equal(
      "Organization/22222222-2222-2222-2222-222222222227",
      (string)rrInformation["performer"] ![0] !["reference"] !);

    Assert.DoesNotContain(resources, resource =>
      (string?)resource["id"] is
        "22222222-2222-2222-2222-222222222221" or
        "22222222-2222-2222-2222-222222222222" or
        "22222222-2222-2222-2222-222222222228" or
        "22222222-2222-2222-2222-222222222229" or
        "22222222-2222-2222-2222-222222222230");

    var composition = resources.Single(resource =>
      (string)resource["resourceType"] ! == "Composition");
    var rrSection = Assert.Single(composition["section"] !.AsArray());
    Assert.Equal("reportability-response-information", (string)rrSection!["id"] !);
    Assert.Equal("Reportability Response Information Section", (string)rrSection!["title"] !);
    Assert.Equal("88085-6", (string)rrSection["code"] !["coding"] ![0] !["code"] !);
    Assert.Equal(
      "Observation/22222222-2222-2222-2222-222222222224",
      (string)rrSection["entry"] ![0] !["reference"] !);
    Assert.Equal(
      "Observation/22222222-2222-2222-2222-222222222223",
      (string)rrSection["extension"] ![1] !["extension"] ![0] !["valueReference"] !["reference"] !);
  }

  [Fact]
  public void ConvertXmlToJson_EnrichesCopiedProcessingStatusExtensionDisplay()
  {
    var actual = JsonNode.Parse(FhirProcessor.ConvertXmlToJson(EicrXml, RrXml)) !;
    var composition = actual["entry"] !.AsArray()
      .Select(entry => entry!["resource"] !)
      .Single(resource => (string)resource["resourceType"] ! == "Composition");
    var rrSection = composition["section"] !.AsArray()
      .Single(section => (string?)section!["title"] == "Reportability Response Information Section");
    var processingStatusExtension = rrSection!["extension"] !.AsArray()
      .Single(extension =>
        (string?)extension!["url"] ==
          "http://hl7.org/fhir/us/ecr/StructureDefinition/rr-eicr-processing-status-extension");
    var processingStatusReference = processingStatusExtension!["extension"] !.AsArray()
      .Single(extension => (string?)extension!["url"] == "eICRProcessingStatus") !["valueReference"] !;

    Assert.Equal("eICR processed", (string)processingStatusReference["display"] !);
  }

  [Fact]
  public void ConvertXmlToJson_Throws_WhenEicrAndRrResourceIdentitiesConflict()
  {
    var conflictingRrXml = RrXml.Replace(
      "urn:uuid:22222222-2222-2222-2222-222222222223",
      "urn:uuid:11111111-1111-1111-1111-111111111113",
      StringComparison.Ordinal);

    var exception = Assert.Throws<Models.UserFacingException>(
      () => FhirProcessor.ConvertXmlToJson(EicrXml, conflictingRrXml));

    Assert.Equal(
      "FHIR eICR and RR Bundles contain conflicting resource identities.",
      exception.Message);
    Assert.Equal(HttpStatusCode.UnprocessableEntity, exception.StatusCode);
  }

  [Fact]
  public void ConvertXmlToJson_Throws_WhenMergeInputsAreSwapped()
  {
    var exception = Assert.Throws<Models.UserFacingException>(
      () => FhirProcessor.ConvertXmlToJson(RrXml, EicrXml));

    Assert.Equal(
      "FHIR eICR input must contain exactly one eICR Composition and one Patient.",
      exception.Message);
    Assert.Equal(HttpStatusCode.UnprocessableEntity, exception.StatusCode);
  }

  [Fact]
  public void ConvertXmlToJson_Throws_WhenInputIsNotValidFhirXml()
  {
    const string invalidFhirXml = "<Bundle xmlns=\"http://hl7.org/fhir\"><entry>";

    var exception = Assert.Throws<Models.UserFacingException>(
      () => FhirProcessor.ConvertXmlToJson(invalidFhirXml));

    Assert.Equal("FHIR XML input must be a valid FHIR R4 Bundle.", exception.Message);
  }

  [Fact]
  public void ConvertXmlToJson_Throws_WhenRrIsNotValidFhirXml()
  {
    var exception = Assert.Throws<Models.UserFacingException>(
      () => FhirProcessor.ConvertXmlToJson(EicrXml, "<not-valid-xml>"));

    Assert.Equal("FHIR RR XML input must be a valid FHIR R4 Bundle.", exception.Message);
  }

  [Fact]
  public void FhirBundlePostProcessing_ShouldAddSourceToMeta_WhenInputTypeIsProvided()
  {
    var fhirInput = File.ReadAllText("../../../../../data/SampleData/FHIR/eCR_EveEverywoman-expected.json");
    var actual = FhirProcessor.FhirBundlePostProcessing(fhirInput);
    var actualJson = JsonNode.Parse(actual);
    var entries = actualJson!["response"]?["FhirResource"]?["entry"] as JsonArray;
    Assert.True(entries?.Count > 0);

    foreach (var entry in entries)
    {
      Assert.NotNull(entry?["resource"]?["meta"]?["source"]);
      Assert.Equal("ecr", (string)entry!["resource"] !["meta"] !["source"] !);
    }
  }
}
