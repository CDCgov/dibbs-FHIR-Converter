using System.Net;
using System.Text.Json.Nodes;
using Dibbs.FhirConverterApi.Processors;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

namespace Dibbs.FhirConverterApi.UnitTests.Processors;

public class FhirProcessorTest
{
  private const string IdentityCollisionMessage =
    "FHIR eICR and RR Bundles contain conflicting resource identities.";

  private const string RrRulesAgencyProfile =
    "http://hl7.org/fhir/us/ecr/StructureDefinition/rr-rules-authoring-agency-organization";

  private const string RetainedResourceTagSystem =
    "https://github.com/CDCgov/dibbs-FHIR-Converter/CodeSystem/fhir-ecr-merger";

  private const string AddedProfileTagSystem =
    "https://github.com/CDCgov/dibbs-FHIR-Converter/CodeSystem/fhir-ecr-merger-added-profile";

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
            <author>
              <reference value="urn:uuid:11111111-1111-1111-1111-111111111112" />
            </author>
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

  private static string AddEntryToEicr(string entry)
  {
    return EicrXml.Replace(
      "</Bundle>",
      $"{entry}\n</Bundle>",
      StringComparison.Ordinal);
  }

  private static string AddSharedRulesAgencyToEicr(string name)
  {
    return AddEntryToEicr($"""
      <entry>
        <fullUrl value="urn:uuid:22222222-2222-2222-2222-222222222227" />
        <resource>
          <Organization>
            <id value="shared-rules-agency" />
            <meta>
              <profile value="http://example.org/fhir/StructureDefinition/eicr-organization" />
            </meta>
            <name value="{name}" />
          </Organization>
        </resource>
      </entry>
      """);
  }

  private static string ConvertJsonBundleToXml(string bundleJson)
  {
    var bundle = new FhirJsonDeserializer(
      new DeserializerSettings().UsingMode(DeserializationMode.Recoverable))
      .Deserialize<Bundle>(bundleJson);
    return new FhirXmlSerializer().SerializeToString(bundle);
  }

  private static string CreatePatientBundleXml(string fullUrl)
  {
    return $"""
      <Bundle xmlns="http://hl7.org/fhir">
        <type value="collection" />
        <entry>
          <fullUrl value="{fullUrl}" />
          <resource>
            <Patient />
          </resource>
        </entry>
      </Bundle>
      """;
  }

  private static void AssertIdentityCollision(string eicrXml, string rrXml)
  {
    var exception = Assert.Throws<Models.UserFacingException>(
      () => FhirProcessor.ConvertXmlToJson(eicrXml, rrXml));

    Assert.Equal(IdentityCollisionMessage, exception.Message);
    Assert.Equal(HttpStatusCode.UnprocessableEntity, exception.StatusCode);
  }

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
    var serialized = FhirProcessor.ConvertXmlToJson(EicrXml, RrXml);
    var actual = JsonNode.Parse(serialized) !;
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

    AssertIdentityCollision(EicrXml, conflictingRrXml);
  }

  [Fact]
  public void ConvertXmlToJson_DeduplicatesEquivalentSharedResourceAndRemapsAllAliases()
  {
    var eicrWithSharedOrganization = AddSharedRulesAgencyToEicr("Rules Agency");
    var rrWithRelativeOrganizationReference = RrXml.Replace(
      """<reference value="urn:uuid:22222222-2222-2222-2222-222222222227" />""",
      """<reference value="Organization/22222222-2222-2222-2222-222222222227" />""",
      StringComparison.Ordinal);

    var actual = JsonNode.Parse(FhirProcessor.ConvertXmlToJson(
      eicrWithSharedOrganization,
      rrWithRelativeOrganizationReference)) !;
    var entries = actual["entry"] !.AsArray();
    var resources = entries.Select(entry => entry!["resource"] !).ToList();

    var organization = Assert.Single(
      resources,
      resource => (string?)resource["resourceType"] == "Organization");
    Assert.Equal("shared-rules-agency", (string)organization["id"] !);

    var profiles = organization["meta"] !["profile"] !.AsArray()
      .Select(profile => (string)profile!)
      .ToList();
    Assert.Contains(
      "http://example.org/fhir/StructureDefinition/eicr-organization",
      profiles);
    Assert.Contains(
      "http://hl7.org/fhir/us/ecr/StructureDefinition/rr-rules-authoring-agency-organization",
      profiles);

    var rrInformation = resources.Single(resource =>
      (string?)resource["id"] == "22222222-2222-2222-2222-222222222225");
    Assert.Equal(
      "Organization/shared-rules-agency",
      (string)rrInformation["performer"] ![0] !["reference"] !);
    Assert.Single(
      entries,
      entry => (string?)entry!["fullUrl"] ==
        "urn:uuid:22222222-2222-2222-2222-222222222227");
  }

  [Fact]
  public void ConvertXmlToJson_PreservesSharedResourceAcrossRepeatedMerge()
  {
    var eicrWithSharedOrganization = AddSharedRulesAgencyToEicr("Rules Agency");
    var firstJson = FhirProcessor.ConvertXmlToJson(eicrWithSharedOrganization, RrXml);
    var firstXml = ConvertJsonBundleToXml(firstJson);

    var second = JsonNode.Parse(FhirProcessor.ConvertXmlToJson(firstXml, RrXml)) !;
    var entries = second["entry"] !.AsArray();
    var resources = entries.Select(entry => entry!["resource"] !).ToList();
    var organization = Assert.Single(
      resources,
      resource => (string?)resource["resourceType"] == "Organization");
    var profiles = organization["meta"] !["profile"] !.AsArray();
    var tags = organization["meta"] !["tag"] !.AsArray();

    Assert.Equal("shared-rules-agency", (string)organization["id"] !);
    Assert.Single(profiles, profile => (string?)profile == RrRulesAgencyProfile);
    Assert.Single(
      tags,
      tag =>
        (string?)tag!["system"] == RetainedResourceTagSystem &&
        (string?)tag["code"] == "retained-eicr-resource");
    Assert.Single(
      tags,
      tag =>
        (string?)tag!["system"] == AddedProfileTagSystem &&
        (string?)tag["code"] == RrRulesAgencyProfile);
    Assert.Single(
      entries,
      entry => (string?)entry!["fullUrl"] ==
        "urn:uuid:22222222-2222-2222-2222-222222222227");

    var rrInformation = resources.Single(resource =>
      (string?)resource["id"] == "22222222-2222-2222-2222-222222222225");
    Assert.Equal(
      "Organization/shared-rules-agency",
      (string)rrInformation["performer"] ![0] !["reference"] !);
  }

  [Fact]
  public void ConvertXmlToJson_PreservesNativeRrProfileAcrossRepeatedMerge()
  {
    var combinedProfiles = $"""
      <profile value="http://example.org/fhir/StructureDefinition/eicr-organization" />
                  <profile value="{RrRulesAgencyProfile}" />
      """;
    var eicrWithNativeRrProfile = AddSharedRulesAgencyToEicr("Rules Agency").Replace(
      """<profile value="http://example.org/fhir/StructureDefinition/eicr-organization" />""",
      combinedProfiles,
      StringComparison.Ordinal);
    var firstJson = FhirProcessor.ConvertXmlToJson(eicrWithNativeRrProfile, RrXml);
    var firstXml = ConvertJsonBundleToXml(firstJson);

    var second = JsonNode.Parse(FhirProcessor.ConvertXmlToJson(firstXml, RrXml)) !;
    var entries = second["entry"] !.AsArray();
    var organization = Assert.Single(
      entries.Select(entry => entry!["resource"] !),
      resource => (string?)resource["resourceType"] == "Organization");
    var profiles = organization["meta"] !["profile"] !.AsArray();
    var tags = organization["meta"] !["tag"] !.AsArray();

    Assert.Equal("shared-rules-agency", (string)organization["id"] !);
    Assert.Single(profiles, profile => (string?)profile == RrRulesAgencyProfile);
    Assert.Single(
      tags,
      tag =>
        (string?)tag!["system"] == RetainedResourceTagSystem &&
        (string?)tag["code"] == "retained-eicr-resource");
    Assert.DoesNotContain(
      tags,
      tag =>
        (string?)tag!["system"] == AddedProfileTagSystem &&
        (string?)tag["code"] == RrRulesAgencyProfile);
  }

  [Fact]
  public void ConvertXmlToJson_DeduplicatesChainedSharedResourcesWhenParentPrecedesChild()
  {
    var eicrWithSharedResources = AddEntryToEicr("""
      <entry>
        <fullUrl value="urn:uuid:22222222-2222-2222-2222-222222222224" />
        <resource>
          <Observation>
            <id value="shared-relevant-condition" />
            <meta>
              <profile value="http://example.org/fhir/StructureDefinition/eicr-observation" />
            </meta>
            <status value="final" />
            <code>
              <coding>
                <system value="http://snomed.info/sct" />
                <code value="64572001" />
              </coding>
            </code>
            <subject>
              <reference value="urn:uuid:11111111-1111-1111-1111-111111111112" />
            </subject>
            <valueCodeableConcept>
              <coding>
                <system value="http://snomed.info/sct" />
                <code value="40468003" />
                <display value="Viral hepatitis, type A" />
              </coding>
            </valueCodeableConcept>
            <hasMember>
              <reference value="Observation/shared-reportability-information" />
            </hasMember>
          </Observation>
        </resource>
      </entry>
      <entry>
        <fullUrl value="urn:uuid:22222222-2222-2222-2222-222222222225" />
        <resource>
          <Observation>
            <id value="shared-reportability-information" />
            <meta>
              <profile value="http://example.org/fhir/StructureDefinition/eicr-observation" />
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
              <reference value="urn:uuid:11111111-1111-1111-1111-111111111112" />
            </subject>
            <performer>
              <reference value="urn:uuid:22222222-2222-2222-2222-222222222227" />
            </performer>
          </Observation>
        </resource>
      </entry>
      """);
    var rrWithRelativeChildReference = RrXml.Replace(
      """<reference value="urn:uuid:22222222-2222-2222-2222-222222222225" />""",
      """<reference value="Observation/22222222-2222-2222-2222-222222222225" />""",
      StringComparison.Ordinal);

    var actual = JsonNode.Parse(FhirProcessor.ConvertXmlToJson(
      eicrWithSharedResources,
      rrWithRelativeChildReference)) !;
    var entries = actual["entry"] !.AsArray();
    var resources = entries.Select(entry => entry!["resource"] !).ToList();
    var retainedParent = resources.Single(resource =>
      (string?)resource["id"] == "shared-relevant-condition");

    Assert.Equal(
      "Observation/shared-reportability-information",
      (string)retainedParent["hasMember"] ![0] !["reference"] !);
    var retainedChild = Assert.Single(
      resources,
      resource => (string?)resource["id"] == "shared-reportability-information");
    Assert.Contains(
      "http://hl7.org/fhir/us/ecr/StructureDefinition/rr-relevant-reportable-condition-observation",
      retainedParent["meta"] !["profile"] !.AsArray().Select(profile => (string)profile!));
    Assert.Contains(
      "http://hl7.org/fhir/us/ecr/StructureDefinition/rr-reportability-information-observation",
      retainedChild["meta"] !["profile"] !.AsArray().Select(profile => (string)profile!));
    Assert.Single(
      entries,
      entry => (string?)entry!["fullUrl"] ==
        "urn:uuid:22222222-2222-2222-2222-222222222224");
    Assert.Single(
      entries,
      entry => (string?)entry!["fullUrl"] ==
        "urn:uuid:22222222-2222-2222-2222-222222222225");
  }

  [Fact]
  public void ConvertXmlToJson_Throws_WhenSharedResourceContentConflicts()
  {
    var eicrWithConflictingOrganization =
      AddSharedRulesAgencyToEicr("Different Rules Agency");

    AssertIdentityCollision(eicrWithConflictingOrganization, RrXml);
  }

  [Fact]
  public void ConvertXmlToJson_Throws_WhenRrContainsAmbiguousResourceIdentity()
  {
    var rrWithDuplicateIdentity = RrXml.Replace(
      """<fullUrl value="urn:uuid:22222222-2222-2222-2222-222222222230" />""",
      """<fullUrl value="urn:uuid:22222222-2222-2222-2222-222222222227" />""",
      StringComparison.Ordinal);

    AssertIdentityCollision(EicrXml, rrWithDuplicateIdentity);
  }

  [Fact]
  public void ConvertXmlToJson_Throws_WhenRrPatientAliasBelongsToAnotherEicrResource()
  {
    var rrWithConflictingPatientFullUrl = RrXml.Replace(
      """<fullUrl value="urn:uuid:22222222-2222-2222-2222-222222222222" />""",
      """<fullUrl value="urn:uuid:11111111-1111-1111-1111-111111111113" />""",
      StringComparison.Ordinal);

    AssertIdentityCollision(EicrXml, rrWithConflictingPatientFullUrl);
  }

  [Fact]
  public void ConvertXmlToJson_UsesRetainedReferenceInGeneratedRrSection()
  {
    var eicrWithSharedCondition = AddEntryToEicr("""
      <entry>
        <fullUrl value="urn:uuid:22222222-2222-2222-2222-222222222224" />
        <resource>
          <Observation>
            <id value="shared-relevant-condition" />
            <meta>
              <profile value="http://example.org/fhir/StructureDefinition/eicr-observation" />
            </meta>
            <status value="final" />
            <code>
              <coding>
                <system value="http://snomed.info/sct" />
                <code value="64572001" />
              </coding>
            </code>
            <subject>
              <reference value="urn:uuid:11111111-1111-1111-1111-111111111112" />
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
      """);

    var actual = JsonNode.Parse(FhirProcessor.ConvertXmlToJson(
      eicrWithSharedCondition,
      RrXml)) !;
    var entries = actual["entry"] !.AsArray();
    var resources = entries.Select(entry => entry!["resource"] !).ToList();
    var composition = resources.Single(resource =>
      (string?)resource["resourceType"] == "Composition");
    var rrSection = composition["section"] !.AsArray()
      .Single(section =>
        (string?)section!["title"] == "Reportability Response Information Section");

    Assert.Equal(
      "Observation/shared-relevant-condition",
      (string)rrSection!["entry"] ![0] !["reference"] !);
    Assert.Single(
      entries,
      entry => (string?)entry!["fullUrl"] ==
        "urn:uuid:22222222-2222-2222-2222-222222222224");
  }

  [Fact]
  public void ConvertXmlToJson_UsesRetainedReferenceInGeneratedProcessingStatusExtension()
  {
    var eicrWithSharedStatus = AddEntryToEicr("""
      <entry>
        <fullUrl value="urn:uuid:22222222-2222-2222-2222-222222222223" />
        <resource>
          <Observation>
            <id value="shared-processing-status" />
            <meta>
              <profile value="http://example.org/fhir/StructureDefinition/eicr-observation" />
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
      """);
    var rrWithoutProcessingStatusExtension = RrXml.Replace(
      "http://hl7.org/fhir/us/ecr/StructureDefinition/rr-eicr-processing-status-extension",
      "http://example.org/fhir/StructureDefinition/not-processing-status",
      StringComparison.Ordinal);

    var actual = JsonNode.Parse(FhirProcessor.ConvertXmlToJson(
      eicrWithSharedStatus,
      rrWithoutProcessingStatusExtension)) !;
    var entries = actual["entry"] !.AsArray();
    var resources = entries.Select(entry => entry!["resource"] !).ToList();
    var retainedStatus = resources.Single(resource =>
      (string?)resource["id"] == "shared-processing-status");
    Assert.Contains(
      "http://hl7.org/fhir/us/ecr/StructureDefinition/rr-eicr-processing-status-observation",
      retainedStatus["meta"] !["profile"] !.AsArray().Select(profile => (string)profile!));

    var composition = resources
      .Single(resource => (string?)resource["resourceType"] == "Composition");
    var rrSection = composition["section"] !.AsArray()
      .Single(section =>
        (string?)section!["title"] == "Reportability Response Information Section");
    var statusExtension = rrSection!["extension"] !.AsArray()
      .Single(extension =>
        (string?)extension!["url"] ==
          "http://hl7.org/fhir/us/ecr/StructureDefinition/rr-eicr-processing-status-extension");

    Assert.Equal(
      "Observation/shared-processing-status",
      (string)statusExtension!["extension"] ![0] !["valueReference"] !["reference"] !);
    Assert.Single(
      entries,
      entry => (string?)entry!["fullUrl"] ==
        "urn:uuid:22222222-2222-2222-2222-222222222223");
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
  public void ConvertXmlToJson_Throws_WhenEicrDoesNotContainPatient()
  {
    var eicrWithoutPatient = EicrXml
      .Replace("<Patient>", "<RelatedPerson>", StringComparison.Ordinal)
      .Replace("</Patient>", "</RelatedPerson>", StringComparison.Ordinal);

    var exception = Assert.Throws<Models.UserFacingException>(
      () => FhirProcessor.ConvertXmlToJson(eicrWithoutPatient, RrXml));

    Assert.Equal(
      "FHIR eICR input must contain exactly one eICR Composition and one Patient.",
      exception.Message);
    Assert.Equal(HttpStatusCode.UnprocessableEntity, exception.StatusCode);
  }

  [Fact]
  public void ConvertXmlToJson_Throws_WhenRrDoesNotContainPatient()
  {
    var rrWithoutPatient = RrXml
      .Replace("<Patient>", "<RelatedPerson>", StringComparison.Ordinal)
      .Replace("</Patient>", "</RelatedPerson>", StringComparison.Ordinal);

    var exception = Assert.Throws<Models.UserFacingException>(
      () => FhirProcessor.ConvertXmlToJson(EicrXml, rrWithoutPatient));

    Assert.Equal(
      "FHIR RR input must contain exactly one RR Composition and one Patient.",
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
  public void ConvertXmlToJson_NormalizesStandaloneFhirEicrBundle_DerivingIdsAndRewritingReferences()
  {
    var actual = JsonNode.Parse(FhirProcessor.ConvertXmlToJson(EicrXml)) !;
    var entries = actual["entry"] !.AsArray();

    Assert.Equal("eicr-bundle", (string)actual["id"] !);
    Assert.Equal(3, entries.Count);

    var composition = entries[0] !["resource"] !;
    Assert.Equal("Composition", (string)composition["resourceType"] !);
    Assert.Equal("Patient/11111111-1111-1111-1111-111111111112", (string)composition["subject"] !["reference"] !);

    var patient = entries[1] !["resource"] !;
    Assert.Equal("Patient", (string)patient["resourceType"] !);
    Assert.Equal("11111111-1111-1111-1111-111111111112", (string)patient["id"] !);

    var observation = entries[2] !["resource"] !;
    Assert.Equal("Observation", (string)observation["resourceType"] !);
    Assert.Equal("11111111-1111-1111-1111-111111111113", (string)observation["id"] !);
    Assert.Equal("Patient/11111111-1111-1111-1111-111111111112", (string)observation["subject"] !["reference"] !);
  }

  [Fact]
  public void ConvertXmlToJson_DerivesResourceIdFromAbsoluteFullUrl()
  {
    var input = CreatePatientBundleXml(
      "https://example.org/fhir/Patient/patient-123");

    var actual = JsonNode.Parse(FhirProcessor.ConvertXmlToJson(input)) !;
    var patientId = (string)actual["entry"] ![0] !["resource"] !["id"] !;

    Assert.True(Id.IsValidValue((string)actual["id"] !));
    Assert.Equal("patient-123", patientId);
  }

  [Fact]
  public void ConvertXmlToJson_GeneratesResourceId_WhenFullUrlHasNoValidId()
  {
    var input = CreatePatientBundleXml(
      "https://example.org/fhir/Patient/not_valid");

    var actual = JsonNode.Parse(FhirProcessor.ConvertXmlToJson(input)) !;
    var patientId = (string)actual["entry"] ![0] !["resource"] !["id"] !;

    Assert.NotEqual("not_valid", patientId);
    Assert.True(Id.IsValidValue(patientId));
  }

  [Fact]
  public void FhirBundlePostProcessing_AddsSourceAndSkipsEntryWithoutResource()
  {
    const string fhirInput = """
      {
        "resourceType": "Bundle",
        "type": "collection",
        "entry": [
          {},
          {
            "resource": {
              "resourceType": "Patient",
              "id": "patient-1"
            }
          },
          {
            "resource": {
              "resourceType": "Patient",
              "id": "patient-2",
              "meta": {
                "profile": [
                  "http://example.org/fhir/StructureDefinition/example-patient"
                ]
              }
            }
          }
        ]
      }
      """;

    var actual = JsonNode.Parse(FhirProcessor.FhirBundlePostProcessing(fhirInput)) !;
    var entries = actual["response"] !["FhirResource"] !["entry"] !.AsArray();

    Assert.Null(entries[0] !["resource"]);
    Assert.Equal("ecr", (string)entries[1] !["resource"] !["meta"] !["source"] !);
    Assert.Equal("ecr", (string)entries[2] !["resource"] !["meta"] !["source"] !);
    Assert.Equal(
      "http://example.org/fhir/StructureDefinition/example-patient",
      (string)entries[2] !["resource"] !["meta"] !["profile"] ![0] !);
  }
}
