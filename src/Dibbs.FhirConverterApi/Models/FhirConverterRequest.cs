using System.Text.Json.Serialization;

namespace Dibbs.FhirConverterApi.Models;

/// <summary>
/// Model for the body of the incoming API request.
/// </summary>
public class FhirConverterRequest
{
    // The message to be converted as a string.
    [JsonPropertyName("input_data")]
    required public string InputData { get; set; }

    // If an eICR message, the accompanying Reportability Response data.
    [JsonPropertyName("rr_data")]
    public string? RRData { get; set; }
}