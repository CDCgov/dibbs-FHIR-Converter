using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Dibbs.FhirConverterApi.Processors;

public class FhirProcessor
{
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
        var bundleJson = JsonNode.Parse(input) !;

        bundleJson = AddDataSourceToBundle(bundleJson);
        var resultsJson = JsonNode.Parse("{\"response\": {\"Status\": \"OK\",\"FhirResource\": {}}}");
        resultsJson!["response"] !["FhirResource"] = bundleJson;
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