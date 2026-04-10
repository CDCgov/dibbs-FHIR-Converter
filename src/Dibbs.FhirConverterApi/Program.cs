using System.Diagnostics;
using System.Net;
using System.Xml.Linq;
using Dibbs.Fhir.Liquid.Converter;
using Dibbs.Fhir.Liquid.Converter.Processors;
using Dibbs.Fhir.Liquid.Converter.Utilities;
using Dibbs.FhirConverterApi;
using Dibbs.FhirConverterApi.Models;
using Dibbs.FhirConverterApi.Processors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;

var dataProcessor = new CcdaProcessor(ConsoleLoggerFactory.CreateLogger<CcdaProcessor>(), TemplateUtility.TemplateOptions);
var templateProvider = new TemplateProvider(TemplateUtility.TemplateDirectory);
var fileProvider = new PhysicalFileProvider(Path.GetFullPath(TemplateUtility.TemplateDirectory));

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Logging.SetMinimumLevel(LogLevel.Information);

var maxRequestBodySizeEnvVar = Environment.GetEnvironmentVariable("MAX_BODY_SIZE");
var maxRequestBodySize = 50 * 1024 * 1024; // 50 MB if no env var set

if (int.TryParse(maxRequestBodySizeEnvVar, out var value))
{
    maxRequestBodySize = value;
}

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = maxRequestBodySize;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configure request logging
app.Use(async (context, next) =>
{
    var logger = context.RequestServices
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("RequestLogger");

    var contentLength = context.Request.ContentLength;

    logger.LogInformation(
        "Incoming request: {method} {path} Content-Length: {length}",
        context.Request.Method,
        context.Request.Path,
        contentLength);

    var sw = Stopwatch.StartNew();

    try
    {
        await next();
        sw.Stop();

        logger.LogInformation(
            "Completed request: {method} {path} Status: {status} Duration: {duration}ms",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            sw.ElapsedMilliseconds);
    }
    catch (Exception ex)
    {
        sw.Stop();

        logger.LogError(
            ex,
            "Request failed: {method} {path} after {duration}ms",
            context.Request.Method,
            context.Request.Path,
            sw.ElapsedMilliseconds);

        throw;
    }
});

app.MapGet("/", () => new { status = "OK" })
.WithName("HealthCheck")
.AddOpenApiOperationTransformer((operation, _, __) =>
   {
       operation.Summary = "Used to confirm that the API is running and responsive.";
       operation.Description = "Returns \"OK\" if the API is running.";
       return Task.CompletedTask;
   });

app.MapPost("/convert-to-fhir", (HttpRequest request, [FromBody] FhirConverterRequest requestBody, ILogger<Program> logger) =>
{
    logger.LogInformation("Entered /convert-to-fhir");
    var sw = Stopwatch.StartNew();
    var inputData = requestBody.InputData;

    logger.LogInformation(
        "InputData length: {length} chars (~{mb} MB)",
        inputData.Length,
        inputData.Length / (1024.0 * 1024.0));
    XDocument ecrDoc;

    try
    {
        logger.LogInformation("Parsing XML...");
        ecrDoc = XDocument.Parse(inputData);
        logger.LogInformation("Parsed XML in {ms}ms", sw.ElapsedMilliseconds);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error parsing XML");
        return Results.Json(new { detail = "EICR message must be valid XML message." }, statusCode: (int)HttpStatusCode.UnprocessableEntity);
    }

    sw.Restart();
    ecrDoc = EcrProcessor.ResolveReferences(ecrDoc);
    logger.LogInformation("Resolved references in {ms}ms", sw.ElapsedMilliseconds);

    if (!string.IsNullOrEmpty(requestBody.RRData))
    {
        try
        {
            sw.Restart();
            ecrDoc = EcrProcessor.MergeEicrAndRR(ecrDoc, requestBody.RRData);
            logger.LogInformation("Merged RR in {ms}ms", sw.ElapsedMilliseconds);
        }
        catch (UserFacingException ex)
        {
            return Results.Json(new { detail = ex.Message }, statusCode: (int)ex.StatusCode);
        }
    }

    inputData = ecrDoc.ToString();

    try
    {
        sw.Restart();
        var result = dataProcessor.Convert(inputData, TemplateUtility.RootTemplate, TemplateUtility.TemplateDirectory, templateProvider, fileProvider);
        logger.LogInformation("Conversion done in {ms}ms", sw.ElapsedMilliseconds);
        sw.Restart();
        var newResult = FhirProcessor.FhirBundlePostProcessing(result);
        logger.LogInformation("Post-processing done in {ms}ms", sw.ElapsedMilliseconds);
        return Results.Text(newResult, contentType: "application/json");
    }
    catch (UserFacingException ex)
    {
        return Results.Json(new { detail = ex.Message }, statusCode: (int)ex.StatusCode);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unhandled exception");
        return Results.Json(new { detail = "Error converting input data." }, statusCode: (int)HttpStatusCode.InternalServerError);
    }
})
.Accepts<dynamic>("application/json")
.WithName("ConvertToFhir")
.AddOpenApiOperationTransformer((operation, _, __) =>
   {
       operation.Summary = "Converts `input_data` from eICR to FHIR.";
       operation.Description = "If applicable, merges eICR and RR and returns converted data as JSON.";
       return Task.CompletedTask;
   });

app.Run();