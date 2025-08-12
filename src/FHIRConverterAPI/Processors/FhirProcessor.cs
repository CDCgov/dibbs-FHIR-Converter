using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Microsoft.Health.Fhir.Liquid.Converter.FHIRConverterAPI.Processors
{
  public class FhirProcessor
  {
    /// <summary>
    ///  Makes final changes to FHIR bundle before returning to caller.
    ///  Replaces Patient resource IDs and adds source info to all resources.
    /// </summary>
    /// <param name="input">The FHIR bundle as a JSON string.</param>
    /// <param name="inputType">The original type of the source data that was converted.</param>
    /// <returns>
    ///  The updated FHIR bundle as a JSON string.
    /// </returns>
    public static string FhirBundlePostProcessing(string input, string inputType)
    {
      var bundleJson = JsonNode.Parse(input)!;
      var oldId = string.Empty;
      var newId = Guid.NewGuid().ToString();

      foreach (var entry in (bundleJson["entry"] as JsonArray) ?? [])
      {
        if ((string)entry!["resource"]!["resourceType"]! == "Patient")
        {
          oldId = (string)entry["resource"]!["id"]!;
          entry["resource"]!["id"] = newId;
          break;
        }
      }

      bundleJson = AddDataSourceToBundle(bundleJson, inputType);
      var resultsJson = JsonNode.Parse("{\"response\": {\"Status\": \"OK\",\"FhirResource\": {}}}");
      resultsJson!["response"]!["FhirResource"] = bundleJson;
      var resultString = resultsJson!.ToJsonString(new JsonSerializerOptions
      {
        WriteIndented = true,

        // Encoder required for HTML sections to be formatted the way we expect
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
      });

      // update references to the patient ID
      resultString = resultString.Replace(oldId, newId);
      return resultString;
    }

    /// <summary>
    ///  Given a FHIR bundle and a data source parameter the function
    ///  will loop through the bundle and add a Meta.source entry for
    ///  every resource in the bundle.
    /// </summary>
    /// <param name="bundle">The FHIR bundle to add minimum provenance to.</param>
    /// <param name="dataSource">The data source of the FHIR bundle.</param>
    /// <returns>
    ///  The FHIR bundle with the a Meta.source entry for each FHIR resource in the bundle
    /// </returns>
    private static JsonNode AddDataSourceToBundle(JsonNode bundle, string dataSource)
    {
      foreach (var entry in (bundle["entry"] as JsonArray) ?? [])
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

        meta["source"] = dataSource;
      }

      return bundle;
    }
  }
}