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


app.MapPost("/convert-to-fhir", async (HttpRequest request, [FromQuery(Name = "input_type")] string input_type = "eCR", [FromQuery(Name = "root_template")] string root_template = "EICR") =>
{
    using var reader = new StreamReader(request.Body);
    var input = await reader.ReadToEndAsync();
    var templatesPath = Environment.GetEnvironmentVariable("TEMPLATES_PATH") ?? "../../data/Templates/";
    var result = ConverterLogicHandler.Convert(templatesPath + input_type, root_template, input, false, false);

    return Results.Text(result, contentType: "application/json");
})
.Accepts<dynamic>("application/xml")
.WithName("ConvertToFhir")
.WithOpenApi();

app.Run();
