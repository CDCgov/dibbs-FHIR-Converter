using System.Text.Json.Serialization;

/// <summary>
/// Model for the body of the incoming API request.
/// </summary>
public class FHIRConverterRequest
{
    // The message to be converted as a string.
    [JsonPropertyName("input_data")]
    required public string InputData { get; set; }

    // The type of message to be converted.
    [JsonPropertyName("input_type")]
    required public string InputType { get; set; }

    // If an eICR message, the accompanying Reportability Response data.
    [JsonPropertyName("rr_data")]
    public string? RRData { get; set; }

    // Name of the liquid template within to be used for conversion.
    [JsonPropertyName("root_template")]
    public string? RootTemplate { get; set; }
}