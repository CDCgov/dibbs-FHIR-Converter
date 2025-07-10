using Microsoft.AspNetCore.Mvc;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

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

// app.UseHttpsRedirection();

app.MapGet("/", () => new { status = "OK" })
.WithName("HealthCheck")
.WithOpenApi();

app.MapPost("/convert-to-fhir", async (HttpRequest request, [FromBody] FHIRConverterRequest requestBody) =>
{
    var templatesPath = Environment.GetEnvironmentVariable("TEMPLATES_PATH") ?? "../../data/Templates/";
    var inputData = requestBody.input_data;
    var inputType = requestBody.input_type.ToLower();
    if (inputType == "vxu" || inputType == "elr")
    {
        inputData = StandardizeHl7DateTimes(inputData);
    }

    try
    {
        if (!string.IsNullOrEmpty(requestBody.rr_data))
        {
            inputData = MergeEicrAndRR(inputData, requestBody.rr_data, inputType);
        }
    }
    catch (Exception ex)
    {
        return Results.BadRequest(ex.Message);
    }

    string rootTemplate;

    try
    {
        rootTemplate = GetRootTemplate(inputType);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(ex.Message);
    }

    var result = ConverterLogicHandler.Convert(GetTemplatesPath(inputType), rootTemplate, inputData, false, false);
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

string MergeEicrAndRR(string inputData, string rrData, string inputType)
{
    if (inputType != "ecr")
    {
        throw new UnprocessableEntityException("Reportability Response (RR) data is only accepted for eCR conversion requests.");
    }

    string mergedEcr;

    try
    {
        mergedEcr = AddRRDataToEicr(inputData, rrData);
    }
    catch (Exception e)
    {
        throw new UnprocessableEntityException("Reportability Response and eICR message both must be valid XML messages.");
    }

    return mergedEcr;
}

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
        throw new Exception($"Invalid input_type {inputType}. Valid values are 'hl7v2' and 'ecr'.");
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

string AddRRDataToEicr(string inputData, string rrData)
{
    // Add xmlns:xsi if missing
    // TODO: Will the schema instance namespace ever actually be missing?
    var ecrLines = inputData.Split(
        ["\n", "\r\n"],
        StringSplitOptions.None).ToList();
    var startIndex = ecrLines.FindIndex(line => line.TrimStart().StartsWith("<ClinicalDocument"));
    var endIndex = ecrLines.FindIndex(startIndex, line => line.TrimEnd().EndsWith('>'));

    if (ecrLines.FindIndex(startIndex, endIndex, line => line.Contains("xmlns:xsi")) == -1)
    {
        var newRootElement = string.Join(" ", ecrLines.ToArray(), startIndex, endIndex - startIndex + 1);
        newRootElement = newRootElement.Replace("xmlns=\"urn:hl7-org:v3\"", "xmlns=\"urn:hl7-org:v3\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"");
        ecrLines.RemoveRange(startIndex, endIndex - startIndex + 1);
        ecrLines.Insert(startIndex, newRootElement);
        inputData = string.Join("\n", ecrLines.ToArray());
    }

    try
    {
        var ecrXDocument = XDocument.Parse(inputData);
        var rrXDocument = XDocument.Parse(rrData);

        // Check for eICR Processing Status entry (required & only available in RR)
        if (ecrXDocument.XPathSelectElement("//*[@root=\"2.16.840.1.113883.10.20.15.2.3.29\"]") is not null)
        {
            Console.WriteLine("This eCR has already been merged with RR data.");
            return ecrXDocument.ToString();
        }

        // If eICR >=R3, remove (optional) RR section that came from eICR
        // This is duplicate/incomplete info from RR
        var ecrVersion = ecrXDocument.XPathEvaluate("string(//*[@root=\"2.16.840.1.113883.10.20.15.2\"]/@extension)");
        if (ecrVersion is not null && DateTime.Parse(ecrVersion.ToString()) >= DateTime.Parse("2021-01-01"))
        {
            var names = new XmlNamespaceManager(ecrXDocument.CreateNavigator().NameTable);
            names.AddNamespace("hl7", "urn:hl7-org:v3");
            var rrFromEicr = ecrXDocument.XPathSelectElement(
                "//hl7:component[hl7:section/hl7:templateId[@root=\"2.16.840.1.113883.10.20.15.2.2.5\"]]",
                names);

            rrFromEicr?.Remove();
        }

        // Create the tags for elements we'll be looking for
        var rrTags = new string[]
        {
            "templateId",
            "id",
            "code",
            "title",
            "effectiveTime",
            "confidentialityCode",
        };

        var rrElements = new List<XElement>();
        var rrNames = new XmlNamespaceManager(rrXDocument.CreateNavigator().NameTable);
        rrNames.AddNamespace("hl7", "urn:hl7-org:v3");

        // Find root-level elements and add them to a list
        foreach (var tag in rrTags)
        {
            var element = rrXDocument.XPathSelectElement($"/*/hl7:{tag}", rrNames);
            if (element is not null)
            {
                rrElements.Add(element);
            }
        }

        // Find the nested entry element that we need
        var rrEntry = rrXDocument.XPathSelectElement(
            $"/*/hl7:component/hl7:structuredBody/hl7:component/hl7:section/hl7:entry" +
            "[@typeCode=\"DRIV\"]/hl7:organizer[@classCode=\"CLUSTER\" and @moodCode=\"EVN\"]/..", rrNames);

        // find the status in the RR utilizing the templateid root
        // codes specified from the APHL/LAC Spec
        var rrEntryForStatusCodes = rrXDocument.XPathSelectElements(
            "/*/hl7:component/hl7:structuredBody/hl7:component/hl7:section/hl7:templateId" +
            "[@root=\"2.16.840.1.113883.10.20.15.2.2.3\"]/../hl7:entry/hl7:act/hl7:templateId[@root=\"2.16.840.1.113883.10.20.15.2.3.29\"]/../..",
            rrNames);

        // Create the section element with root-level elements
        // and entry to insert in the eICR
        if (rrEntry is not null)
        {
            var ecrSection = new XElement("section");
            foreach (var element in rrElements)
            {
                ecrSection.Add(element);
            }

            if (rrEntryForStatusCodes is not null)
            {
                ecrSection.Add(rrEntryForStatusCodes);
            }

            ecrSection.Add(rrEntry);

            // Append the ecr section into the eCR - puts it at the end
            ecrXDocument.Root.Add(ecrSection);
        }

        return ecrXDocument.ToString();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error processing eICR document: {ex.Message}");
        throw new Exception($"Error processing eICR document: {ex.Message}");
    }
}

public partial class Program
{
    // Expose the Program class for use with WebApplicationFactory<T>
}