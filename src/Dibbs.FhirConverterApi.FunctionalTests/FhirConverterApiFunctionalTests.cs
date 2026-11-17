using System.Net;
using System.Net.Http.Json;
using System.Xml.Linq;
using Dibbs.Fhir.Liquid.Converter;
using Dibbs.Fhir.Liquid.Converter.Processors;
using Dibbs.Fhir.Liquid.Converter.Utilities;
using Dibbs.FhirConverterApi.Models;
using Dibbs.FhirConverterApi.Processors;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Snapshooter.Xunit;

namespace Dibbs.FhirConverterApi.FunctionalTests;

public class FhirConverterApiFunctionalTests : IClassFixture<WebApplicationFactory<Program>>
{
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
    public async Task ConvertToFhir_WithoutDocumentIdAndWithRr_MatchesStringConversion()
    {
        var document = XDocument.Load("../../../../../data/SampleData/eCR/yoda_eICR.xml");
        document.Root!.Elements(document.Root.Name.Namespace + "id").Remove();
        var input = document.ToString(SaveOptions.DisableFormatting);
        var rr = File.ReadAllText("../../../../../data/SampleData/eCR/yoda_RR.xml");
        document = EcrProcessor.MergeEicrAndRR(EcrProcessor.ResolveReferences(document), rr);
        var processor = new CcdaProcessor(NullLogger<CcdaProcessor>.Instance, TemplateUtility.TemplateOptions);
        var templates = new TemplateProvider(TemplateUtility.TemplateDirectory);
        using var files = new PhysicalFileProvider(Path.GetFullPath(TemplateUtility.TemplateDirectory));
        var expected = FhirProcessor.FhirBundlePostProcessing(processor.Convert(
            document.ToString(SaveOptions.DisableFormatting), "EICR", TemplateUtility.TemplateDirectory, templates, files));

        var response = await _client.PostAsJsonAsync("/convert-to-fhir", new FhirConverterRequest { InputData = input, RRData = rr });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expected, await response.Content.ReadAsStringAsync());
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
