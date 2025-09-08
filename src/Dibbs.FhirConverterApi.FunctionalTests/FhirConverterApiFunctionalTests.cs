using System.Net;
using System.Net.Http.Json;
using Dibbs.FhirConverterApi.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Snapshooter.Xunit;

namespace Dibbs.FhirConverterApi.FunctionalTests;

public class FhirConverterApiFunctionalTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public FhirConverterApiFunctionalTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
        Environment.SetEnvironmentVariable("TEMPLATES_PATH", "../../../../../data/Templates/");
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
            InputType = "eCR",
            InputData = eICR,
            RRData = rr,
        };

        var response = await _client.PostAsync("/convert-to-fhir", JsonContent.Create(content));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var jsonResponse = await response.Content.ReadAsStringAsync();
        Snapshot.Match(jsonResponse);
    }

    [Fact]
    public async Task ConvertToFhir_ReturnsSuccess_WhenValidEicrWithoutRrProvided()
    {
        var eICR = File.ReadAllText("../../../../../data/SampleData/eCR/yoda_eICR.xml");
        var content = new FhirConverterRequest
        {
            InputType = "eCR",
            InputData = eICR,
        };

        var response = await _client.PostAsync("/convert-to-fhir", JsonContent.Create(content));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var jsonResponse = await response.Content.ReadAsStringAsync();
        Snapshot.Match(jsonResponse);
    }

    [Fact]
    public async Task ConvertToFhir_Returns422StatusCode_WhenInvalidEicrProvided()
    {
        var rr = File.ReadAllText("../../../../../data/SampleData/eCR/yoda_RR.xml");
        var content = new FhirConverterRequest
        {
            InputType = "eCR",
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
            InputType = "eCR",
            InputData = eICR,
            RRData = "<this is not valid xml>",
        };

        var response = await _client.PostAsync("/convert-to-fhir", JsonContent.Create(content));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var jsonResponse = await response.Content.ReadAsStringAsync();
        Assert.Equal("{\"detail\":\"Reportability Response (RR) message must be valid XML message.\"}", jsonResponse);
    }

    [Fact]
    public async Task ConvertToFhir_Returns400StatusCode_WhenRrProvidedWithoutWrongInputDataType()
    {
        var vxu = File.ReadAllText("../../../../../data/SampleData/Hl7v2/VXU-Sample.hl7");
        var rr = File.ReadAllText("../../../../../data/SampleData/eCR/yoda_RR.xml");
        var content = new FhirConverterRequest
        {
            InputType = "vxu",
            InputData = vxu,
            RRData = rr,
            RootTemplate = "VXU_V04",
        };

        var response = await _client.PostAsync("/convert-to-fhir", JsonContent.Create(content));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var jsonResponse = await response.Content.ReadAsStringAsync();
        Assert.Equal("{\"detail\":\"Reportability Response (RR) data is only accepted for eCR conversion requests.\"}", jsonResponse);
    }

    [Fact]
    public async Task ConvertToFhir_Returns400StatusCode_WhenInvalidTemplateDirectoryProvided()
    {
        var eICR = File.ReadAllText("../../../../../data/SampleData/eCR/yoda_eICR.xml");
        var content = new FhirConverterRequest
        {
            InputType = "badinputtype",
            InputData = eICR,
            RootTemplate = "EICR",
        };

        var response = await _client.PostAsync("/convert-to-fhir", JsonContent.Create(content));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var jsonResponse = await response.Content.ReadAsStringAsync();
        Assert.Equal("{\"detail\":\"Invalid input_type badinputtype. Valid values are 'ecr', 'elr', and 'vxu'.\"}", jsonResponse);
    }

    [Fact]
    public async Task ConvertToFhir_Returns400StatusCode_WhenRRProvidedWhenInvalidInputTypeProvided()
    {
        var content = new FhirConverterRequest
        {
            InputType = "ccd",
            InputData = "<ClinicalDocument></ClinicalDocument>",
        };

        var response = await _client.PostAsync("/convert-to-fhir", JsonContent.Create(content));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var jsonResponse = await response.Content.ReadAsStringAsync();
        Assert.Equal("{\"detail\":\"Root template for ccd cannot be found. Please specify using the root_template parameter.\"}", jsonResponse);
    }

    [Fact]
    public async Task ConvertToFhir_ReturnsSuccess_WhenValidVxuProvided()
    {
        var vxu = File.ReadAllText("../../../../../data/SampleData/Hl7v2/VXU-Sample.hl7");
        var content = new FhirConverterRequest
        {
            InputType = "vxu",
            InputData = vxu,
            RootTemplate = "VXU_V04",
        };

        var response = await _client.PostAsync("/convert-to-fhir", JsonContent.Create(content));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var jsonResponse = await response.Content.ReadAsStringAsync();

        // Ignore provenance div because of generated on timestamp
        Snapshot.Match(jsonResponse, matchOptions => matchOptions.IgnoreField("response.FhirResource.entry[1].resource.text.div"));
    }
}
