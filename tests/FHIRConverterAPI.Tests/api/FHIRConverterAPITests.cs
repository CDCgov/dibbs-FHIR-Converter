using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
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
        var xmlPayload = "<ClinicalDocument xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"></ClinicalDocument>";
        var content = new StringContent(xmlPayload, Encoding.UTF8, "application/xml");

        var response = await _client.PostAsync("/convert-to-fhir", content);

        var jsonResponse = await response.Content.ReadAsStringAsync();
        Assert.NotNull(jsonResponse);
        Assert.Contains("\"resourceType\": \"Bundle\"", jsonResponse);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
