using System.Net;
using System.Xml.Linq;
using Dibbs.Fhir.Liquid.Converter;
using Dibbs.Fhir.Liquid.Converter.Processors;
using Dibbs.Fhir.Liquid.Converter.Utilities;
using Dibbs.FhirConverterApi;
using Dibbs.FhirConverterApi.Models;
using Dibbs.FhirConverterApi.Processors;
using Microsoft.AspNetCore.Mvc;

var dataProcessor = new CcdaProcessor(ConsoleLoggerFactory.CreateLogger<CcdaProcessor>());
var templateProvider = new TemplateProvider(TemplateUtility.TemplateDirectory);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => new { status = "OK" })
.WithName("HealthCheck")
.WithOpenApi();

app.MapPost("/convert-to-fhir", (HttpRequest request, [FromBody] FhirConverterRequest requestBody) =>
{
    var inputData = requestBody.InputData;
    var inputType = requestBody.InputType.ToLower();
    XDocument ecrDoc;

    try
    {
        ecrDoc = XDocument.Parse(inputData);
    }
    catch (Exception ex)
    {
        Console.WriteLine("Ex: {1} StackTrace: '{0}'", Environment.StackTrace, ex);
        return Results.Json(new { detail = "EICR message must be valid XML message." }, statusCode: (int)HttpStatusCode.UnprocessableEntity);
    }

    ecrDoc = EcrProcessor.ResolveReferences(ecrDoc);

    if (!string.IsNullOrEmpty(requestBody.RRData))
    {
        try
        {
            ecrDoc = EcrProcessor.MergeEicrAndRR(ecrDoc, requestBody.RRData);
        }
        catch (UserFacingException ex)
        {
            return Results.Json(new { detail = ex.Message }, statusCode: (int)ex.StatusCode);
        }
    }

    inputData = ecrDoc.ToString();

    try
    {
        var result = dataProcessor.Convert(inputData, TemplateUtility.RootTemplate, TemplateUtility.TemplateDirectory, templateProvider);
        var newResult = FhirProcessor.FhirBundlePostProcessing(result, inputType);
        return Results.Text(newResult, contentType: "application/json");
    }
    catch (UserFacingException ex)
    {
        return Results.Json(new { detail = ex.Message }, statusCode: (int)ex.StatusCode);
    }
    catch (Exception ex)
    {
        Console.WriteLine("Ex: {1} StackTrace: '{0}'", Environment.StackTrace, ex);
        return Results.Json(new { detail = "Error converting input data." }, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.Accepts<dynamic>("application/json")
.WithName("ConvertToFhir")
.WithOpenApi();

app.Run();

public partial class Program
{
    // Expose the Program class for use with WebApplicationFactory<T>
}