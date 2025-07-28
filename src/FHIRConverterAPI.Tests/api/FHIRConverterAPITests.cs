using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Snapshooter.Xunit;

public class FHIRConverterAPITests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public FHIRConverterAPITests(WebApplicationFactory<Program> factory)
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
    public async Task ConvertToFHIR_ReturnsSuccess_WhenValidEICRWithRRProvided()
    {
        Environment.SetEnvironmentVariable("TEMPLATES_PATH", "../../../../../data/Templates/");
        var eICR = File.ReadAllText("../../../../../data/SampleData/eCR/yoda_eICR.xml");
        var rr = File.ReadAllText("../../../../../data/SampleData/eCR/yoda_RR.xml");
        var content = new FHIRConverterRequest
        {
            input_type = "eCR",
            input_data = eICR,
            rr_data = rr,
        };

        var response = await _client.PostAsync("/convert-to-fhir", JsonContent.Create(content));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var jsonResponse = await response.Content.ReadAsStringAsync();
        Assert.NotNull(jsonResponse);

        Snapshot.Match(jsonResponse, CommonIgnoredFields);
    }

    [Fact]
    public async Task ConvertToFHIR_ReturnsSuccess_WhenValidEICRWithoutRRProvided()
    {
        Environment.SetEnvironmentVariable("TEMPLATES_PATH", "../../../../../data/Templates/");
        var eICR = File.ReadAllText("../../../../../data/SampleData/eCR/yoda_eICR.xml");
        var content = new FHIRConverterRequest
        {
            input_type = "eCR",
            input_data = eICR,
        };

        var response = await _client.PostAsync("/convert-to-fhir", JsonContent.Create(content));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var jsonResponse = await response.Content.ReadAsStringAsync();
        Assert.NotNull(jsonResponse);

        File.WriteAllText("actual.json", jsonResponse);

        Snapshot.Match(jsonResponse, CommonIgnoredFields);
    }

    [Fact]
    public async Task ConvertToFHIR_Returns422StatusCode_WhenInvalidEICRProvided()
    {
        Environment.SetEnvironmentVariable("TEMPLATES_PATH", "../../../../../data/Templates/");
        var rr = File.ReadAllText("../../../../../data/SampleData/eCR/yoda_RR.xml");
        var content = new FHIRConverterRequest
        {
            input_type = "eCR",
            input_data = "<this is not valid xml>",
            rr_data = rr,
        };

        var response = await _client.PostAsync("/convert-to-fhir", JsonContent.Create(content));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var jsonResponse = await response.Content.ReadAsStringAsync();
        Assert.Equal("{\"detail\":\"EICR message must be valid XML message.\"}", jsonResponse);
    }

    [Fact]
    public async Task ConvertToFHIR_Returns422StatusCode_WhenInvalidRRProvided()
    {
        Environment.SetEnvironmentVariable("TEMPLATES_PATH", "../../../../../data/Templates/");
        var eICR = File.ReadAllText("../../../../../data/SampleData/eCR/yoda_eICR.xml");
        var content = new FHIRConverterRequest
        {
            input_type = "eCR",
            input_data = eICR,
            rr_data = "<this is not valid xml>",
        };

        var response = await _client.PostAsync("/convert-to-fhir", JsonContent.Create(content));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var jsonResponse = await response.Content.ReadAsStringAsync();
        Assert.Equal("{\"detail\":\"Reportability Response (RR) message must be valid XML message.\"}", jsonResponse);
    }

    // TODO: more error response tests

    [Fact]
    public async Task ConvertToFHIR_ReturnsSuccess_WhenValidVXUProvided()
    {
        Environment.SetEnvironmentVariable("TEMPLATES_PATH", "../../../../../data/Templates/");
        var vxu = File.ReadAllText("../../../../../data/SampleData/Hl7v2/VXU-Sample.hl7");
        var content = new FHIRConverterRequest
        {
            input_type = "vxu",
            input_data = vxu,
            root_template = "VXU_V04",
        };

        var response = await _client.PostAsync("/convert-to-fhir", JsonContent.Create(content));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var jsonResponse = await response.Content.ReadAsStringAsync();
        Assert.NotNull(jsonResponse);

        // find diff
        // var expectedObject = JObject.Parse(expected);
        // var actualObject = JObject.Parse(jsonResponse);

        // var diff = DiffHelper.FindDiff(actualObject, expectedObject);
        // if (diff.HasValues)
        // {
        //     Console.WriteLine(diff);
        // }
        // find diff

        Snapshot.Match(jsonResponse, matchOptions => CommonIgnoredFields(matchOptions)
                    // Ignore provenance div because of generated on timestamp
                    .IgnoreField("response.FhirResource.entry[1].resource.text.div"));
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
