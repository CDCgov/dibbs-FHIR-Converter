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

app.MapPost("/convert-to-fhir", async (HttpRequest request, [FromBody] FHIRConverterRequest requestBody) =>
{
    var templatesPath = Environment.GetEnvironmentVariable("TEMPLATES_PATH") ?? "../../data/Templates/";
    var result = ConverterLogicHandler.Convert(templatesPath + requestBody.input_type, requestBody.root_template, requestBody.input_data, false, false);

    return Results.Text(result, contentType: "application/json");
})
.Accepts<dynamic>("application/json")
.WithName("ConvertToFhir")
.WithOpenApi();

app.Run();

public partial class Program
{
    // Expose the Program class for use with WebApplicationFactory<T>
}