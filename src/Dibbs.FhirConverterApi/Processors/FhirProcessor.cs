using System.Buffers;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml;
using Dibbs.FhirConverterApi.Models;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

namespace Dibbs.FhirConverterApi.Processors;

public class FhirProcessor
{
    private static readonly JsonSerializerOptions ResponseSerializerOptions = new ()
    {
        WriteIndented = true,

        // Encoder required for HTML sections to be formatted the way we expect
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static readonly JsonWriterOptions FhirResponseWriterOptions = new ()
    {
        Indented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    // TODO: remove deserialization mode eventually.
    // This is a permissive setting to allow invalid test data through.
    private static readonly FhirXmlDeserializer SyntaxOnlyDeserializer = new (
        new DeserializerSettings()
            .UsingMode(DeserializationMode.Recoverable));

    /// <summary>
    /// Converts a FHIR R4 XML Bundle to its normalized FHIR JSON representation.
    /// Derives logical IDs for resources without an ID and rewrites internal
    /// references to relative ResourceType/id references.
    /// </summary>
    /// <param name="input">The FHIR R4 XML Bundle.</param>
    /// <returns>The normalized FHIR Bundle as a JSON string.</returns>
    /// <exception cref="UserFacingException">Thrown when the input is not a valid FHIR R4 Bundle.</exception>
    public static string ConvertXmlToJson(string input)
    {
        return FhirJsonSerializer.Default.SerializeToString(ConvertXmlToBundle(input));
    }

    /// <summary>
    /// Converts a FHIR R4 XML Bundle to a normalized Bundle POCO.
    /// </summary>
    /// <param name="input">The FHIR R4 XML Bundle.</param>
    /// <returns>The normalized FHIR Bundle.</returns>
    internal static Bundle ConvertXmlToBundle(string input)
    {
        var bundle = DeserializeBundle(input, "FHIR XML input must be a valid FHIR R4 Bundle.");
        return FhirEcrMerger.Normalize(bundle);
    }

    /// <summary>
    /// Converts a FHIR R4 XML reader to a normalized Bundle POCO.
    /// </summary>
    /// <param name="input">A reader positioned on the FHIR Bundle root element.</param>
    /// <returns>The normalized FHIR Bundle.</returns>
    internal static Bundle ConvertXmlToBundle(XmlReader input)
    {
        var bundle = DeserializeBundle(input, "FHIR XML input must be a valid FHIR R4 Bundle.");
        return FhirEcrMerger.Normalize(bundle);
    }

    /// <summary>
    /// Converts separate FHIR R4 eICR and RR XML Bundles into the flat JSON Bundle
    /// consumed by the eCR Viewer.
    /// </summary>
    /// <param name="eicrInput">The FHIR R4 eICR document Bundle.</param>
    /// <param name="rrInput">The FHIR R4 Reportability Response document Bundle.</param>
    /// <returns>The combined eICR Bundle as a JSON string.</returns>
    /// <exception cref="UserFacingException">Thrown when either input is not a valid FHIR R4 Bundle.</exception>
    public static string ConvertXmlToJson(string eicrInput, string rrInput)
    {
        return FhirJsonSerializer.Default.SerializeToString(
            ConvertXmlToBundle(eicrInput, rrInput));
    }

    /// <summary>
    /// Converts and merges separate FHIR R4 eICR and RR XML Bundles into a
    /// single Bundle POCO without an intermediate JSON representation.
    /// </summary>
    /// <param name="eicrInput">The FHIR R4 eICR document Bundle.</param>
    /// <param name="rrInput">The FHIR R4 Reportability Response document Bundle.</param>
    /// <returns>The combined eICR Bundle.</returns>
    internal static Bundle ConvertXmlToBundle(string eicrInput, string rrInput)
    {
        var eicrBundle = DeserializeBundle(
            eicrInput,
            "FHIR XML input must be a valid FHIR R4 Bundle.");
        var rrBundle = DeserializeBundle(
            rrInput,
            "FHIR RR XML input must be a valid FHIR R4 Bundle.");

        return FhirEcrMerger.Merge(eicrBundle, rrBundle);
    }

    /// <summary>
    /// Converts and merges a FHIR eICR reader and serialized RR Bundle.
    /// </summary>
    /// <param name="eicrInput">A reader positioned on the FHIR eICR Bundle root element.</param>
    /// <param name="rrInput">The FHIR R4 Reportability Response document Bundle.</param>
    /// <returns>The combined eICR Bundle.</returns>
    internal static Bundle ConvertXmlToBundle(XmlReader eicrInput, string rrInput)
    {
        var eicrBundle = DeserializeBundle(
            eicrInput,
            "FHIR XML input must be a valid FHIR R4 Bundle.");
        var rrBundle = DeserializeBundle(
            rrInput,
            "FHIR RR XML input must be a valid FHIR R4 Bundle.");

        return FhirEcrMerger.Merge(eicrBundle, rrBundle);
    }

    private static Bundle DeserializeBundle(string input, string errorMessage)
    {
        try
        {
            return SyntaxOnlyDeserializer.Deserialize<Bundle>(input);
        }
        catch (Exception ex)
        {
            throw new UserFacingException(
                errorMessage,
                HttpStatusCode.UnprocessableEntity,
                ex);
        }
    }

    private static Bundle DeserializeBundle(XmlReader input, string errorMessage)
    {
        try
        {
            return SyntaxOnlyDeserializer.Deserialize<Bundle>(input);
        }
        catch (Exception ex)
        {
            throw new UserFacingException(
                errorMessage,
                HttpStatusCode.UnprocessableEntity,
                ex);
        }
    }

    /// <summary>
    ///  Makes final changes to FHIR bundle before returning to caller.
    ///  Adds source info to all resources.
    /// </summary>
    /// <param name="input">The FHIR bundle as a JSON string.</param>
    /// <returns>
    ///  The updated FHIR bundle as a JSON string.
    /// </returns>
    public static string FhirBundlePostProcessing(string input)
    {
        var bundleJson = JsonNode.Parse(input) ?? new JsonObject();
        return FhirBundlePostProcessing(bundleJson);
    }

    /// <summary>
    /// Makes final changes to a FHIR Bundle JSON object before returning it to the caller.
    /// </summary>
    /// <param name="bundleJson">The mutable FHIR Bundle JSON object.</param>
    /// <returns>The wrapped API response serialized as JSON.</returns>
    internal static string FhirBundlePostProcessing(JsonNode bundleJson)
    {
        AddDataSourceToBundle(bundleJson);
        var resultsJson = new JsonObject
        {
            ["response"] = new JsonObject
            {
                ["Status"] = "OK",
                ["FhirResource"] = bundleJson,
            },
        };

        return resultsJson.ToJsonString(ResponseSerializerOptions);
    }

    /// <summary>
    /// Adds source metadata to a typed FHIR Bundle and serializes it directly
    /// into the API response envelope.
    /// </summary>
    /// <param name="bundle">The normalized or merged FHIR Bundle.</param>
    /// <returns>The wrapped API response serialized as JSON.</returns>
    internal static string FhirBundlePostProcessing(Bundle bundle)
    {
        AddDataSourceToBundle(bundle);

        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer, FhirResponseWriterOptions))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("response");
            writer.WriteStartObject();
            writer.WriteString("Status", "OK");
            writer.WritePropertyName("FhirResource");
            FhirJsonSerializer.Default.Serialize(bundle, writer);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    /// <summary>
    ///  Given a FHIR bundle and a data source parameter the function
    ///  will loop through the bundle and add a Meta.source entry for
    ///  every resource in the bundle.
    /// </summary>
    /// <param name="bundle">The FHIR bundle to add minimum provenance to.</param>
    private static void AddDataSourceToBundle(JsonNode bundle)
    {
        foreach (var entry in (bundle["entry"] as JsonArray) ?? new JsonArray())
        {
            var resource = entry!["resource"];
            if (resource is null)
            {
                continue;
            }

            JsonNode? meta = resource["meta"];

            if (meta is null)
            {
                meta = new JsonObject();
                resource["meta"] = meta;
            }

            meta["source"] = "ecr";
        }
    }

    private static void AddDataSourceToBundle(Bundle bundle)
    {
        foreach (var entry in bundle.Entry)
        {
            if (entry.Resource is not { } resource)
            {
                continue;
            }

            resource.Meta ??= new Meta();
            resource.Meta.SourceElement ??= new FhirUri();
            resource.Meta.SourceElement.Value = "ecr";
        }
    }
}
