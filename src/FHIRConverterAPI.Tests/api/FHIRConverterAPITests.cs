using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using System.Text.Json.Nodes;

public class FHIRConverterAPITests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public FHIRConverterAPITests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ConvertToFHIR_ReturnsSuccess_WhenValidXMLProvided()
    {
        Environment.SetEnvironmentVariable("TEMPLATES_PATH", "../../../../../data/Templates/");
        var workingDirectory = Environment.CurrentDirectory;
        var eICR = File.ReadAllText("../../../../../data/SampleData/eCR/yoda_eICR.xml");
        var rr = File.ReadAllText("../../../../../data/SampleData/eCR/yoda_RR.xml");
        var content = new FHIRConverterRequest
        {
            input_type = "eCR",
            input_data = eICR,
            rr_data = rr
        };

        var response = await _client.PostAsync("/convert-to-fhir", JsonContent.Create(content));
        var jsonResponse = await response.Content.ReadAsStringAsync();

        var responseJNode = JsonNode.Parse(jsonResponse);
        var entries = responseJNode["response"]["FhirResource"]["entry"] as JsonArray;
        var patientId = (string)entries.Where(x => (string)x["resource"]["resourceType"] == "Patient").First()["resource"]["id"];

        // Hack to deal with new ID being generated every time
        jsonResponse = jsonResponse.Replace(patientId, "b326e36e-b4ef-4bd3-ac4a-1aee81d10665");

        Assert.NotNull(jsonResponse);

        var expected = File.ReadAllText("../../../../../data/SampleData/FHIR/YodaEcrBundle.json");
        Assert.Equal(expected, jsonResponse);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
