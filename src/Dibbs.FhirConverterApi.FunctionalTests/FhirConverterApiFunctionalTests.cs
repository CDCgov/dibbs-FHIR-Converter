using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Dibbs.FhirConverterApi.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Snapshooter.Xunit;

namespace Dibbs.FhirConverterApi.FunctionalTests;

public class FhirConverterApiFunctionalTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string ValidFhirXml = """
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

    private const string ValidFhirEicrXml = """
        <Bundle xmlns="http://hl7.org/fhir">
          <id value="eicr-bundle" />
          <type value="document" />
          <entry>
            <fullUrl value="urn:uuid:11111111-1111-1111-1111-111111111111" />
            <resource>
              <Composition>
                <id value="eicr-composition" />
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
                <id value="eicr-patient" />
              </Patient>
            </resource>
          </entry>
        </Bundle>
        """;

    private const string ValidFhirRrXml = """
        <Bundle xmlns="http://hl7.org/fhir">
          <id value="rr-bundle" />
          <type value="document" />
          <entry>
            <fullUrl value="urn:uuid:22222222-2222-2222-2222-222222222221" />
            <resource>
              <Composition>
                <id value="rr-composition" />
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
              </Composition>
            </resource>
          </entry>
          <entry>
            <fullUrl value="urn:uuid:22222222-2222-2222-2222-222222222222" />
            <resource>
              <Patient>
                <id value="rr-patient" />
              </Patient>
            </resource>
          </entry>
          <entry>
            <fullUrl value="urn:uuid:22222222-2222-2222-2222-222222222223" />
            <resource>
              <Observation>
                <id value="rr-status" />
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
                <id value="rr-condition" />
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
              </Observation>
            </resource>
          </entry>
        </Bundle>
        """;

    private readonly HttpClient _client;

    public FhirConverterApiFunctionalTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck()
    {
        var response = await _client.GetAsync("/");
        var jsonResponse = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("{\"status\":\"OK\"}", jsonResponse);
    }

    [Fact]
    public async Task OpenApi()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ConvertToFhir_ReturnsSuccess_WhenValidEicrWithRrProvided()
    {
        var eICR = File.ReadAllText("../../../../../data/SampleData/eCR/yoda_eICR.xml");
        var rr = File.ReadAllText("../../../../../data/SampleData/eCR/yoda_RR.xml");
        var content = new FhirConverterRequest
        {
            InputData = eICR,
            RRData = rr,
        };

        var response = await _client.PostAsync("/convert-to-fhir", JsonContent.Create(content));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var jsonResponse = await response.Content.ReadAsStringAsync();
        Snapshot.Match(jsonResponse, matchOptions => CommonIgnoredFields(matchOptions));
    }

    [Fact]
    public async Task ConvertToFhir_ReturnsSuccess_WhenValidEicrWithoutRrProvided()
    {
        var eICR = File.ReadAllText("../../../../../data/SampleData/eCR/yoda_eICR.xml");
        var content = new FhirConverterRequest
        {
            InputData = eICR,
        };

        var response = await _client.PostAsync("/convert-to-fhir", JsonContent.Create(content));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var jsonResponse = await response.Content.ReadAsStringAsync();
        Snapshot.Match(jsonResponse, matchOptions => CommonIgnoredFields(matchOptions));
    }

    [Fact]
    public async Task ConvertToFhir_ReturnsSuccess_WhenValidFhirXmlProvided()
    {
        var content = new FhirConverterRequest
        {
            InputData = ValidFhirXml,
        };

        var response = await _client.PostAsync("/convert-to-fhir", JsonContent.Create(content));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var jsonResponse = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        var bundle = jsonResponse!["response"] !["FhirResource"] !;
        Assert.Equal("OK", (string)jsonResponse["response"] !["Status"] !);
        Assert.Equal("Bundle", (string)bundle["resourceType"] !);
        Assert.Equal(
            "urn:uuid:12345678-1234-1234-1234-123456789abc",
            (string)bundle["identifier"] !["value"] !);
        Assert.Equal("ecr", (string)bundle["entry"] ![0] !["resource"] !["meta"] !["source"] !);
    }

    [Fact]
    public async Task ConvertToFhir_ReturnsSuccess_WhenValidFhirEicrWithoutRrProvided()
    {
        var content = new FhirConverterRequest
        {
            InputData = ValidFhirEicrXml,
        };

        var response = await _client.PostAsync("/convert-to-fhir", JsonContent.Create(content));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var jsonResponse = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        var bundle = jsonResponse!["response"] !["FhirResource"] !;
        var entries = bundle["entry"] !.AsArray();

        Assert.Equal("OK", (string)jsonResponse["response"] !["Status"] !);
        Assert.Equal("Bundle", (string)bundle["resourceType"] !);
        Assert.Equal("document", (string)bundle["type"] !);
        Assert.Equal(2, entries.Count);

        var composition = entries[0] !["resource"] !;
        Assert.Equal("Composition", (string)composition["resourceType"] !);
        Assert.Equal("Patient/eicr-patient", (string)composition["subject"] !["reference"] !);

        var patient = entries[1] !["resource"] !;
        Assert.Equal("Patient", (string)patient["resourceType"] !);
        Assert.Equal("eicr-patient", (string)patient["id"] !);
        Assert.Equal("ecr", (string)patient["meta"] !["source"] !);
    }

    [Fact]
    public async Task ConvertToFhir_ReturnsSuccess_WhenSeparateFhirEicrAndRrProvided()
    {
        var content = new FhirConverterRequest
        {
            InputData = ValidFhirEicrXml,
            RRData = ValidFhirRrXml,
        };

        var response = await _client.PostAsync("/convert-to-fhir", JsonContent.Create(content));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var jsonResponse = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        var bundle = jsonResponse!["response"] !["FhirResource"] !;
        var entries = bundle["entry"] !.AsArray();
        var patientEntry = Assert.Single(
            entries,
            entry => (string?)entry?["resource"]?["resourceType"] == "Patient");
        Assert.Single(
            entries,
            entry => (string?)entry?["resource"]?["resourceType"] == "Composition");
        var rrConditionEntry = Assert.Single(
            entries,
            entry =>
                entry?["resource"]?["meta"]?["profile"]?.ToJsonString().Contains(
                    "rr-relevant-reportable-condition-observation",
                    StringComparison.Ordinal) == true);
        var patientReference = $"Patient/{(string)patientEntry!["resource"] !["id"] !}";

        Assert.Equal("OK", (string)jsonResponse["response"] !["Status"] !);
        Assert.Equal("Bundle", (string)bundle["resourceType"] !);
        Assert.Equal("document", (string)bundle["type"] !);
        Assert.Equal(
            patientReference,
            (string)rrConditionEntry!["resource"] !["subject"] !["reference"] !);
        Assert.DoesNotContain(entries, entry =>
            (string?)entry?["resource"]?["resourceType"] is "Bundle" or "MessageHeader");
    }

    [Fact]
    public async Task ConvertToFhir_Returns422StatusCode_WhenMalformedFhirRrProvided()
    {
        var content = new FhirConverterRequest
        {
            InputData = ValidFhirEicrXml,
            RRData = "<this is not valid xml>",
        };

        var response = await _client.PostAsync("/convert-to-fhir", JsonContent.Create(content));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var jsonResponse = await response.Content.ReadAsStringAsync();
        Assert.Equal(
            "{\"detail\":\"FHIR RR XML input must be a valid FHIR R4 Bundle.\"}",
            jsonResponse);
    }

    [Fact]
    public async Task ConvertToFhir_Returns422StatusCode_WhenXmlRootIsUnsupported()
    {
        var content = new FhirConverterRequest
        {
            InputData = "<Patient xmlns=\"http://hl7.org/fhir\" />",
        };

        var response = await _client.PostAsync("/convert-to-fhir", JsonContent.Create(content));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var jsonResponse = await response.Content.ReadAsStringAsync();
        Assert.Equal(
            "{\"detail\":\"Unsupported XML root element. Expected a C-CDA ClinicalDocument or FHIR R4 Bundle.\"}",
            jsonResponse);
    }

    [Fact]
    public async Task ConvertToFhir_Returns422StatusCode_WhenInvalidEicrProvided()
    {
        var rr = File.ReadAllText("../../../../../data/SampleData/eCR/yoda_RR.xml");
        var content = new FhirConverterRequest
        {
            InputData = "<this is not valid xml>",
            RRData = rr,
        };

        var response = await _client.PostAsync("/convert-to-fhir", JsonContent.Create(content));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var jsonResponse = await response.Content.ReadAsStringAsync();
        Assert.Equal("{\"detail\":\"EICR message must be valid XML message.\"}", jsonResponse);
    }

    [Fact]
    public async Task ConvertToFhir_Returns422StatusCode_WhenInvalidRrProvided()
    {
        var eICR = File.ReadAllText("../../../../../data/SampleData/eCR/yoda_eICR.xml");
        var content = new FhirConverterRequest
        {
            InputData = eICR,
            RRData = "<this is not valid xml>",
        };

        var response = await _client.PostAsync("/convert-to-fhir", JsonContent.Create(content));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var jsonResponse = await response.Content.ReadAsStringAsync();
        Assert.Equal("{\"detail\":\"Reportability Response (RR) message must be valid XML message.\"}", jsonResponse);
    }

    private static Snapshooter.MatchOptions CommonIgnoredFields(Snapshooter.MatchOptions matchOptions)
    {
        return matchOptions
                    .IgnoreAllFields("id")
                    .IgnoreAllFields("fullUrl")
                    .IgnoreAllFields("reference")
                    .IgnoreField("response.FhirResource.entry[*].request.url");
    }
}
