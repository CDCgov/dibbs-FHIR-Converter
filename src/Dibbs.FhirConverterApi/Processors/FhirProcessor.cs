using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Dibbs.FhirConverterApi.Models;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

namespace Dibbs.FhirConverterApi.Processors;

public class FhirProcessor
{
    // TODO: remove deserialization mode eventually.
    // This is a permissive setting to allow invalid test data through.
    private static readonly FhirXmlDeserializer SyntaxOnlyDeserializer = new (
        new DeserializerSettings()
            .UsingMode(DeserializationMode.Recoverable));

    /// <summary>
    /// Converts a FHIR R4 XML Bundle to its FHIR JSON representation.
    /// </summary>
    /// <param name="input">The FHIR R4 XML Bundle.</param>
    /// <returns>The FHIR Bundle as a JSON string.</returns>
    /// <exception cref="UserFacingException">Thrown when the input is not a valid FHIR R4 Bundle.</exception>
    public static string ConvertXmlToJson(string input)
    {
        return DeserializeBundle(input, "FHIR XML input must be a valid FHIR R4 Bundle.").ToJson();
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

        bundleJson = AddDataSourceToBundle(bundleJson);
        var resultsJson = JsonNode.Parse("{\"response\": {\"Status\": \"OK\",\"FhirResource\": {}}}") ?? new JsonObject();
        resultsJson["response"] !["FhirResource"] = bundleJson;
        var resultString = resultsJson!.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = true,

            // Encoder required for HTML sections to be formatted the way we expect
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        });

        return resultString;
    }

    /// <summary>
    ///  Given a FHIR bundle and a data source parameter the function
    ///  will loop through the bundle and add a Meta.source entry for
    ///  every resource in the bundle.
    /// </summary>
    /// <param name="bundle">The FHIR bundle to add minimum provenance to.</param>
    /// <returns>
    ///  The FHIR bundle with the a Meta.source entry for each FHIR resource in the bundle
    /// </returns>
    private static JsonNode AddDataSourceToBundle(JsonNode bundle)
    {
        foreach (var entry in (bundle["entry"] as JsonArray) ?? new JsonArray())
        {
            var resource = entry!["resource"];
            if (resource is null)
            {
                return bundle;
            }

            JsonNode? meta = resource["meta"];

            if (meta is null)
            {
                meta = new JsonObject();
                resource["meta"] = meta;
            }

            meta["source"] = "ecr";
        }

        return bundle;
    }
}
