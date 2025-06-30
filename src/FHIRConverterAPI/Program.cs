using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add support for xml
builder.Services.AddControllers().AddXmlSerializerFormatters();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/", () => new { status = "OK" })
.WithName("HealthCheck")
.WithOpenApi();

app.MapPost("/convert-to-fhir", async (HttpRequest request, [FromBody] FHIRConverterRequest requestBody) =>
{
    var templatesPath = Environment.GetEnvironmentVariable("TEMPLATES_PATH") ?? "../../data/Templates/";
    var inputData = requestBody.input_data;
    if (requestBody.input_type == "vxu" || requestBody.input_type == "elr")
    {
        inputData = StandardizeHl7DateTimes(inputData);
    }

    try
    {
        if (string.IsNullOrEmpty(requestBody.rr_data))
        {
            inputData = MergeEicrAndRR(requestBody.input_data, requestBody.rr_data, requestBody.input_type);
        }
    }
    catch (Exception ex)
    {
        return Results.BadRequest(ex.Message);
    }

    string rootTemplate;

    try
    {
        rootTemplate = GetRootTemplate(requestBody.input_type);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(ex.Message);
    }

    var result = ConverterLogicHandler.Convert(GetTemplatesPath(requestBody.input_type), rootTemplate, inputData, false, false);

    return Results.Text(result, contentType: "application/json");
})
.Accepts<dynamic>("application/json")
.WithName("ConvertToFhir")
.WithOpenApi();

app.Run();

string StandardizeHl7DateTimes(string inputData)
{
    return inputData;
}

string MergeEicrAndRR(string input_data, string rr_data, string input_type)
{
    if (input_type != "ecr")
    {
        throw new UnprocessableEntityException("Reportability Response (RR) data is only accepted for eCR conversion requests.");
    }

    string mergedEcr;

    try
    {
        mergedEcr = AddRRDataToEicr(input_data, rr_data);
    }
    catch (Exception e)
    {
        throw new UnprocessableEntityException("Reportability Response and eICR message both must be valid XML messages.");
    }

    return mergedEcr;
}

string GetTemplatesPath(string inputType)
{
    if (inputType == "vxu" || inputType == "elr")
    {
        return "/build/FHIR-Converter/data/Templates/Hl7v2";
    }
    else if (inputType == "ecr")
    {
        return "/build/FHIR-Converter/data/Templates/eCR";
    }
    else
    {
        throw new Exception("Invalid input_type " + inputType + ". Valid values are 'hl7v2' and 'ecr'.");
    }
}

string GetRootTemplate(string inputType)
{
    return inputType switch
    {
        "ecr" => "EICR",
        "elr" => "ORU_R01",
        "vxu" => "VXU_V04",
        "fhir" => string.Empty,
        _ => throw new NotImplementedException($"The conversion from data type {inputType} to FHIR is not supported")
    };
}

string AddRRDataToEicr(string input_data, string rr_data)
{
    return input_data;
}

public partial class Program
{
    // Expose the Program class for use with WebApplicationFactory<T>
}