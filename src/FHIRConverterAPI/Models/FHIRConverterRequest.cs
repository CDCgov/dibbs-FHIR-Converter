public class FHIRConverterRequest
{
    // The message to be converted as a string.
    public string input_data { get; set; }

    // The type of message to be converted.
    public string input_type { get; set; }

    // If an eICR message, the accompanying Reportability Response data.
    public string rr_data { get; set; }
}