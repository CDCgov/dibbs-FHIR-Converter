using Microsoft.AspNetCore.Mvc;
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

app.UseHttpsRedirection();

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
        StringSplitOptions.None
    ).ToList();
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
        var defaultNamespace = ecrXDocument.Root?.GetDefaultNamespace().NamespaceName;

        // Check for eICR Processing Status entry (required & only available in RR)
        if (ecrXDocument.XPathSelectElement("//*[@root=\"2.16.840.1.113883.10.20.15.2.3.29\"]") is not null)
        {
            Console.WriteLine("This eCR has already been merged with RR data.");
            return ecrXDocument.ToString();
        }

        // If eICR >=R3, remove (optional) RR section that came from eICR
        // This is duplicate/incomplete info from RR
        var ecrVersion = ecrXDocument.XPathEvaluate("string(//*[@root=\"2.16.840.1.113883.10.20.15.2\"]/@extension)").ToString();
        Console.WriteLine(ecrVersion);
        // if (ecrVersion >= "2021-01-01")
        // {
        //     namespaces = {"hl7": "urn:hl7-org:v3"}
        //     rr_from_eicr_arr = ecr.xpath(
        //         '//hl7:component[hl7:section/hl7:templateId[@root="2.16.840.1.113883.10.20.15.2.2.5" and @extension="2021-01-01"]]',
        //         namespaces=namespaces,
        //     )
        // }

        //     if rr_from_eicr_arr:
        //         rr_from_eicr = rr_from_eicr_arr[0]
        //         rr_parent = rr_from_eicr.getparent()
        //         if rr_parent is not None:
        //             rr_parent.remove(rr_from_eicr)

        // # Create the tags for elements we'll be looking for
        // rr_tags = [
        //     "templateId",
        //     "id",
        //     "code",
        //     "title",
        //     "effectiveTime",
        //     "confidentialityCode",
        // ]
        // rr_tags = ["{urn:hl7-org:v3}" + tag for tag in rr_tags]
        // rr_elements = []

        // # Find root-level elements and add them to a list
        // for tag in rr_tags:
        //     rr_elements.append(rr.find(f"./{tag}", namespaces=rr.nsmap))

        // # Find the nested entry element that we need
        // entry_tag = "{urn:hl7-org:v3}" + "component/structuredBody/component/section/entry"
        // rr_nested_entries = rr.findall(f"./{entry_tag}", namespaces=rr.nsmap)

        // organizer_tag = "{urn:hl7-org:v3}" + "organizer"

        // # For now we assume there is only one matching entry
        // rr_entry = None
        // for entry in rr_nested_entries:
        //     if entry.attrib and "DRIV" in entry.attrib["typeCode"]:
        //         organizer = entry.find(f"./{organizer_tag}", namespaces=entry.nsmap)
        //         if (
        //             organizer is not None
        //             and "CLUSTER" in organizer.attrib["classCode"]
        //             and "EVN" in organizer.attrib["moodCode"]
        //         ):
        //             rr_entry = entry
        //             exit

        // # find the status in the RR utilizing the templateid root
        // # codes specified from the APHL/LAC Spec
        // base_tag_for_status = (
        //     "{urn:hl7-org:v3}" + "component/structuredBody/component/section"
        // )
        // template_id_tag = "{urn:hl7-org:v3}" + "templateId"
        // entry_status_tag = "{urn:hl7-org:v3}" + "entry"
        // act_status_tag = "{urn:hl7-org:v3}" + "act"
        // sections_for_status = rr.findall(f"./{base_tag_for_status}", namespaces=rr.nsmap)
        // rr_entry_for_status_codes = None
        // for status_section in sections_for_status:
        //     template_id = status_section.find(
        //         f"./{template_id_tag}", namespaces=status_section.nsmap
        //     )
        //     if (
        //         template_id is not None
        //         and "2.16.840.1.113883.10.20.15.2.2.3" in template_id.attrib["root"]
        //     ):
        //         for entry in status_section.findall(
        //             f"./{entry_status_tag}", namespaces=status_section.nsmap
        //         ):
        //             for act in entry.findall(f"./{act_status_tag}", namespaces=entry.nsmap):
        //                 entry_act_template_id = act.find(
        //                     f"./{template_id_tag}", namespaces=act.nsmap
        //                 )
        //                 if (
        //                     entry_act_template_id is not None
        //                     and "2.16.840.1.113883.10.20.15.2.3.29"
        //                     in entry_act_template_id.attrib["root"]
        //                 ):
        //                     # only anticipating one status code
        //                     rr_entry_for_status_codes = entry
        //                     exit

        // # Create the section element with root-level elements
        // # and entry to insert in the eICR
        // ecr_section = None
        // if rr_entry is not None:
        //     ecr_section_tag = "{urn:hl7-org:v3}" + "section"
        //     ecr_section = etree.Element(ecr_section_tag)
        //     ecr_section.extend(rr_elements)
        //     if rr_entry_for_status_codes is not None:
        //         ecr_section.append(rr_entry_for_status_codes)
        //     ecr_section.append(rr_entry)

        //     # Append the ecr section into the eCR - puts it at the end
        //     ecr.append(ecr_section)

        // ecr = etree.tostring(ecr, encoding="unicode", method="xml")

        // return ecr
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error processing eICR document: {ex.Message}");
        throw new Exception($"Error processing eICR document: {ex.Message}");
    }

    return inputData;
}

public partial class Program
{
    // Expose the Program class for use with WebApplicationFactory<T>
}