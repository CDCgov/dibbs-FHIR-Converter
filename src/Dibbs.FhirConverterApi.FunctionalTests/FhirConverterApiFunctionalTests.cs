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
    public async Task ConvertToFhir_Returns422StatusCode_WhenFhirXmlAndRrProvided()
    {
        var content = new FhirConverterRequest
        {
            InputData = ValidFhirXml,
            RRData = File.ReadAllText("../../../../../data/SampleData/eCR/yoda_RR.xml"),
        };

        var response = await _client.PostAsync("/convert-to-fhir", JsonContent.Create(content));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var jsonResponse = await response.Content.ReadAsStringAsync();
        Assert.Equal(
            "{\"detail\":\"Reportability Response (RR) data is only supported for C-CDA input.\"}",
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
