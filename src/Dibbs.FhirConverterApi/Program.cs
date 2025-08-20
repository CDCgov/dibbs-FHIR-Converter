using System.Net;
using System.Xml.Linq;
using Dibbs.FhirConverterApi.Models;
using Dibbs.FhirConverterApi.Processors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Health.Fhir.Liquid.Converter.Tool;

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

    if (!string.IsNullOrEmpty(requestBody.RRData) && inputType != "ecr")
    {
        return Results.BadRequest(new { detail = "Reportability Response (RR) data is only accepted for eCR conversion requests." });
    }

    if (inputType == "ecr")
    {
        try
        {
            XDocument ecrDoc = EcrProcessor.ConvertStringToXDocument(inputData);

            if (!string.IsNullOrEmpty(requestBody.RRData))
            {
                ecrDoc = EcrProcessor.MergeEicrAndRR(ecrDoc, requestBody.RRData);
            }

            inputData = ecrDoc.ToString();
        }
        catch (UserFacingException ex)
        {
            return Results.Json(new { detail = ex.Message }, statusCode: (int)ex.StatusCode);
        }
    }

    if (inputType == "vxu" || inputType == "elr")
    {
        inputData = Hl7Processor.StandardizeHl7DateTimes(inputData);
    }

    string rootTemplate;

    try
    {
        rootTemplate = requestBody.RootTemplate ?? GetRootTemplate(inputType);
        var result = ConverterLogicHandler.ConvertWithoutSaving(GetTemplatesPath(inputType), rootTemplate, inputData, false, false);
        var newResult = FhirProcessor.FhirBundlePostProcessing(result, inputType);
        return Results.Text(newResult, contentType: "application/json");
    }
    catch (UserFacingException ex)
    {
        return Results.Json(new { detail = ex.Message }, statusCode: (int)ex.StatusCode);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
        return Results.StatusCode(500);
    }
})
.Accepts<dynamic>("application/json")
.WithName("ConvertToFhir")
.WithOpenApi();

app.Run();

string GetTemplatesPath(string inputType)
{
    var templatesDir = Environment.GetEnvironmentVariable("TEMPLATES_PATH") ?? "../../data/Templates/";
    if (inputType == "vxu" || inputType == "elr")
    {
        return templatesDir + "/Hl7v2";
    }
    else if (inputType == "ecr")
    {
        return templatesDir + "/eCR";
    }
    else
    {
        throw new UserFacingException($"Invalid input_type {inputType}. Valid values are 'ecr', 'elr', and 'vxu'.", HttpStatusCode.BadRequest);
    }
}

string GetRootTemplate(string inputType)
{
    return inputType switch
    {
        "ecr" => "EICR",
        "elr" => "ORU_R01",
        "vxu" => "VXU_V04",
        _ => throw new UserFacingException($"Root template for {inputType} cannot be found. Please specify using the root_template parameter.", HttpStatusCode.BadRequest)
    };
}

public partial class Program
{
    // Expose the Program class for use with WebApplicationFactory<T>
}