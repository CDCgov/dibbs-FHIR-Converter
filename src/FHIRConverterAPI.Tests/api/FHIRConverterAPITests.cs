using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

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
        var xmlPayload = File.ReadAllText("../../../../../data/SampleData/eCR/eCR_EveEverywoman.xml");
        var content = new FHIRConverterRequest
        {
            input_type = "eCR",
            input_data = xmlPayload,
            root_template = "EICR"
        };

        var response = await _client.PostAsync("/convert-to-fhir", JsonContent.Create(content));

        var jsonResponse = await response.Content.ReadAsStringAsync();
        Assert.NotNull(jsonResponse);

        var expected = File.ReadAllText("../../../../../src/Microsoft.Health.Fhir.Liquid.Converter.FunctionalTests/TestData/Expected/eCR/EICR/eCR_EveEverywoman-expected.json");
        Assert.Equal(expected, jsonResponse);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
