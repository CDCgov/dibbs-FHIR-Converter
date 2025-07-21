using System.Text.RegularExpressions;
using Efferent.HL7.V2;

namespace Microsoft.Health.Fhir.Liquid.Converter.FHIRConverterAPI.Processors
{
  public class Hl7Processor
  {
    /// <summary>
    ///  Prepares an HL7 message for conversion by normalizing / sanitizing
    ///  fields that are known to contain datetime data in problematic formats.
    ///  This function helps messages conform to expectations.
    ///
    ///  This function accepts either segments terminated by `\\r` or `\\n`, but always
    ///  returns data with `\\n` as the segment terminator.
    /// </summary>
    /// <param name="inputData">The raw HL7 message to sanitize.</param>
    /// <returns>
    ///  The HL7 message with potential problem formats resolved.
    ///  If the function is unable to parse a date, the original value is retained.
    /// </returns>
    public static string StandardizeHl7DateTimes(string inputData)
    {
      try
      {
        // The hl7 module requires \n characters be replaced with \r
        var message = new Message(inputData.Replace("\n", "\r"));
        message.ParseMessage();

        // MSH-7 - Message date/time
        message = NormalizeHl7DatetimeSegment(message, "MSH", fieldList: [7]);

        // PID-7 - Date of Birth
        // PID-29 - Date of Death
        // PID-33 - Last update date/time
        message = NormalizeHl7DatetimeSegment(message, "PID", fieldList: [7, 29, 33]);

        // PV1-44 - Admission Date
        // PV1-45 - Discharge Date
        message = NormalizeHl7DatetimeSegment(message, "PV1", fieldList: [44, 45]);

        // ORC-9 Date/time of transaction
        // ORC-15 Order effective date/time
        // ORC-27 Filler's expected availability date/time
        message = NormalizeHl7DatetimeSegment(message, "ORC", fieldList: [9, 15, 27]);

        // OBR-7 Observation date/time
        // OBR-8 Observation end date/time
        // OBR-22 Status change date/time
        // OBR-36 Scheduled date/time
        message = NormalizeHl7DatetimeSegment(message, "OBR", fieldList: [7, 8, 22, 36]);

        // OBX-12 Effective date/time of reference range
        // OBX-14 Date/time of observation
        // OBX-19 Date/time of analysis
        message = NormalizeHl7DatetimeSegment(message, "OBX", fieldList: [12, 14, 19]);

        // TQ1-7 Start date/time
        // TQ1-8 End date/time
        message = NormalizeHl7DatetimeSegment(message, "TQ1", fieldList: [7, 8]);

        // SPM-18 Specimen received date/time
        // SPM-19 Specimen expiration date/time
        message = NormalizeHl7DatetimeSegment(message, "SPM", fieldList: [18, 19]);

        // RXA-3 Date/time start of administration
        // RXA-4 Date/time end of administration
        // RXA-16 Substance expiration date
        // RXA-22 System entry date/time
        message = NormalizeHl7DatetimeSegment(message, "RXA", fieldList: [3, 4, 16, 22]);

        return message.SerializeMessage().Replace("\r", "\n");
      }
      catch (Exception ex)
      {
        // @TODO: Eliminate logging, raise an exception, document the exception
        // in the docstring, and make this fit into our new structure of allowing
        // the caller to implement more robust error handling
        Console.WriteLine("Exception occurred while cleaning message. Passing through original message.");
        return inputData;
      }
    }

    /// <summary>
    ///  Splits HL7 datetime-formatted fields into the following parts:
    ///  <integer 8+ digits>[.<integer 1+ digits>][+/-<integer 4+ digits>]
    ///
    ///  Each group of integers is truncated to conform to the HL7
    ///  specification:
    ///
    ///  - first integer group: max 14 digits
    ///  - following decimal point: max 4 digits
    ///  - following +/- (timezone): 4 digits
    ///
    ///  This normalization facilitates downstream processing using
    ///  cloud providers that have particular requirements for dates.
    /// </summary>
    /// <param name="hl7Datetime">The raw datetime string to clean.</param>
    /// <returns>
    ///  The datetime string with normalizing substitutions
    ///  performed, or the original HL7 datetime if no matching
    ///  format could be found.
    /// </returns>
    private static string NormalizeHl7Datetime(string hl7Datetime)
    {
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

    /// <summary>
    ///  Applies datetime normalization to multiple fields in a segment,
    ///  overwriting values in the input segment as necessary.
    /// </summary>
    /// <param name="message">
    ///  The HL7 message, represented as a list of indexable component strings
    ///  (which is how the HL7 library processes and returns messages).
    /// </param>
    /// <param name="segmentId">The segment type (MSH, PID, etc) of the field to replace.</param>
    /// <param name="fieldList">
    ///  The list of field numbers to replace in the segment named by `segmentId`.
    /// </param>
    /// <returns>The updated HL7 message.</returns>
    private static Message NormalizeHl7DatetimeSegment(Message message, string segmentId, List<int> fieldList)
    {
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
      catch (IndexOutOfRangeException ex)
      {
        // @TODO: Eliminate logging, raise an exception, document the exception
        // in the docstring, and make this fit into our new structure of allowing
        // the caller to implement more robust error handling
        Console.WriteLine($"Segment {segmentId} not found in message.");
      }

      // TODO: verify that we end up back in the foreachloop if we get an exception
      return message;
    }
  }
}