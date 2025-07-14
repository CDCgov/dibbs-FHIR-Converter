using Microsoft.AspNetCore.Mvc;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Efferent.HL7.V2;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Json.Nodes;

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
    var newResult = DoStuff(result, inputType);

    return Results.Text(newResult, contentType: "application/json");
})
.Accepts<dynamic>("application/json")
.WithName("ConvertToFhir")
.WithOpenApi();

app.Run();

string DoStuff(string input, string inputType)
{
    var resultJson = JsonNode.Parse(input)!;
    var oldId = string.Empty;
    var newId = Guid.NewGuid().ToString();

    foreach (var entry in (resultJson["entry"] as JsonArray) ?? [])
    {
        if ((string)entry["resource"]!["resourceType"]! == "Patient")
        {
            oldId = (string)entry["resource"]!["id"]!;
            entry["resource"]!["id"] = newId;
            break;
        }
    }

    resultJson = AddDataSourceToBundle(resultJson, inputType);

    var resultString = resultJson!.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    resultString.Replace(oldId, newId);
    return resultString;
}

string NormalizeHl7Datetime(string hl7Datetime)
{
    // """
    // Splits HL7 datetime-formatted fields into the following parts:
    // <integer 8+ digits>[.<integer 1+ digits>][+/-<integer 4+ digits>]

    // Each group of integers is truncated to conform to the HL7
    // specification:

    // - first integer group: max 14 digits
    // - following decimal point: max 4 digits
    // - following +/- (timezone): 4 digits

    // This normalization facilitates downstream processing using
    // cloud providers that have particular requirements for dates.

    // :param hl7_datetime: The raw datetime string to clean.
    // :return: The datetime string with normalizing substitutions
    //   performed, or the original HL7 datetime if no matching
    //   format could be found.
    // """

    var datetimeRegex = new Regex(@"(\d{8}\d*)(\.\d+)?([+-]\d+)?");
    var hl7DatetimeMatch = datetimeRegex.Match(hl7Datetime);

    if (hl7DatetimeMatch.Success == false)
    {
        return hl7Datetime;
    }

    var hl7DatetimeParts = hl7DatetimeMatch.Groups.Values;

    // Start with date base
    // The first group is always the whole string so we start with the second
    var endIndex = hl7DatetimeParts.ElementAtOrDefault(1)?.Value.Length >= 14 ? 14 : hl7DatetimeParts.ElementAtOrDefault(1)?.Value.Length ?? 0;
    var normalizedDatetime = hl7DatetimeParts.ElementAtOrDefault(1)?.Value[..endIndex]; // probably delete all the distinct stuff

    // Add date decimal if present
    if (!string.IsNullOrEmpty(hl7DatetimeParts.ElementAtOrDefault(2)?.Value))
    {
        var decimalEndIndex = hl7DatetimeParts.ElementAt(2).Value.Length >= 5 ? 5 : hl7DatetimeParts.ElementAt(2).Value.Length;
        normalizedDatetime += hl7DatetimeParts.ElementAt(2).Value[..decimalEndIndex];  // . plus first 4 digits
    }

    // Add timezone information if present
    if (!string.IsNullOrEmpty(hl7DatetimeParts.ElementAtOrDefault(3)?.Value) && hl7DatetimeParts.ElementAt(3).Value.Length >= 5)
    {
        normalizedDatetime += hl7DatetimeParts.ElementAt(3).Value[..5]; // +/- plus 4 digits
    }

    return normalizedDatetime ?? hl7Datetime;
}

void NormalizeHl7DatetimeSegment(Message message, string segmentId, List<int> fieldList)
{ 
    // """
    // Applies datetime normalization to multiple fields in a segment,
    // overwriting values in the input segment as necessary.

    // :param message: The HL7 message, represented as a list
    //   of indexable component strings (which is how the HL7 library
    //   processes and returns messages).
    // :param segment_id: The segment type (MSH, PID, etc) of the field to replace.
    // :param field_num: The field number to replace in the segment named by `segment_id`.
    // :param field_list: The list of field numbers to replace in the segment named
    //   by `segement_id`.
    // """
    try
    {
        foreach (var segment in message.Segments(segmentId))
        {
            foreach (var fieldNum in fieldList)
            {
                var fields = segment.GetAllFields();
                // Datetime value is always in first component
                if (fields.Count > fieldNum && fields[fieldNum].Value != string.Empty)
                {
                    var cleanedDatetime = NormalizeHl7Datetime(fields[fieldNum].Value);
                    fields[fieldNum].Value = cleanedDatetime;
                }
            }
        }
    }
    // # @TODO: Eliminate logging, raise an exception, document the exception
    // # in the docstring, and make this fit into our new structure of allowing
    // # the caller to implement more robust error handling
    catch (IndexOutOfRangeException ex)
    {
        Console.WriteLine($"Segment {segmentId} not found in message.");
    }
}

string StandardizeHl7DateTimes(string inputData)
{
    try
    {
        //The hl7 module requires \n characters be replaced with \r
        var message = new Message(inputData.Replace("\n", "\r"));
        message.ParseMessage();

        // MSH-7 - Message date/time
        NormalizeHl7DatetimeSegment(message, "MSH", fieldList: [7]);

        // PID-7 - Date of Birth
        // PID-29 - Date of Death
        // PID-33 - Last update date/time
        NormalizeHl7DatetimeSegment(message, "PID", fieldList: [7, 29, 33]);

        // PV1-44 - Admission Date
        // PV1-45 - Discharge Date
        NormalizeHl7DatetimeSegment(message, "PV1", fieldList: [44, 45]);

        // ORC-9 Date/time of transaction
        // ORC-15 Order effective date/time
        // ORC-27 Filler's expected availability date/time
        NormalizeHl7DatetimeSegment(message, "ORC", fieldList: [9, 15, 27]);

        // OBR-7 Observation date/time
        // OBR-8 Observation end date/time
        // OBR-22 Status change date/time
        // OBR-36 Scheduled date/time
        NormalizeHl7DatetimeSegment(message, "OBR", fieldList: [7, 8, 22, 36]);

        // OBX-12 Effective date/time of reference range
        // OBX-14 Date/time of observation
        // OBX-19 Date/time of analysis
        NormalizeHl7DatetimeSegment(message, "OBX", fieldList: [12, 14, 19]);

        // TQ1-7 Start date/time
        // TQ1-8 End date/time
        NormalizeHl7DatetimeSegment(message, "TQ1", fieldList: [7, 8]);

        // SPM-18 Specimen received date/time
        // SPM-19 Specimen expiration date/time
        NormalizeHl7DatetimeSegment(message, "SPM", fieldList: [18, 19]);

        // RXA-3 Date/time start of administration
        // RXA-4 Date/time end of administration
        // RXA-16 Substance expiration date
        // RXA-22 System entry date/time
        NormalizeHl7DatetimeSegment(message, "RXA", fieldList: [3, 4, 16, 22]);

        Console.WriteLine(message.SerializeMessage());
        return message.SerializeMessage().Replace("\r", "\n");
    }
    catch (Exception ex)
    {
        // # @TODO: Eliminate logging, raise an exception, document the exception
        // # in the docstring, and make this fit into our new structure of allowing
        // # the caller to implement more robust error handling
        Console.WriteLine("Exception occurred while cleaning message. Passing through original message.");
        return inputData;
    }
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
            "/*/hl7:component/hl7:structuredBody/hl7:component/hl7:section/hl7:entry" +
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

JsonNode AddDataSourceToBundle(JsonNode bundle, string dataSource)
{
    // """
    // Given a FHIR bundle and a data source parameter the function
    // will loop through the bundle and add a Meta.source entry for
    // every resource in the bundle.

    // :param bundle: The FHIR bundle to add minimum provenance to.
    // :param data_source: The data source of the FHIR bundle.
    // :return: The FHIR bundle with the a Meta.source entry for each
    //   FHIR resource in the bundle
    // """
    if (dataSource == string.Empty)
    {
        throw new Exception("The dataSource parameter must be a defined, non-empty string.");
    }

    foreach (var entry in (bundle["entry"] as JsonArray) ?? [])
    {
        var resource = entry["resource"];
        if (resource is null)
        {
            return bundle;
        }

        // TODO: make real types, I need to be able to updates these objects and I can't with trygetpropertyvalue
        JsonNode meta = resource["meta"] ?? JsonNode.Parse("{}");
        meta["source"] = dataSource;
    }

    return bundle;
}

public partial class Program
{
    // Expose the Program class for use with WebApplicationFactory<T>
}