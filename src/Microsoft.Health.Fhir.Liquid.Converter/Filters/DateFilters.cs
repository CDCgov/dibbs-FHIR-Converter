// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Health.Fhir.Liquid.Converter.Exceptions;
using Microsoft.Health.Fhir.Liquid.Converter.Models;

namespace Microsoft.Health.Fhir.Liquid.Converter
{
    /// <summary>
    /// Filters for conversion
    /// </summary>
    public partial class Filters
    {

        private static TimeZoneHandlingMethod ParseTimeZone(string timeZoneHandling)
        {
            if (!Enum.TryParse(timeZoneHandling, true, out TimeZoneHandlingMethod outputTimeZoneHandling))
            {
                throw new RenderException(FhirConverterErrorCode.InvalidTimeZoneHandling, Resources.InvalidTimeZoneHandling);
            }

            return outputTimeZoneHandling;
        }

        private static PartialDateTime ParsePartialDate(string input, DateTimeType timeType)
        {
            try
            {
                return new PartialDateTime(input, timeType);
            }
            catch (Exception)
            {
                throw new RenderException(FhirConverterErrorCode.InvalidDateTimeFormat, string.Format(Resources.InvalidDateTimeFormat, input));
            }
        }

        public static string AddSeconds(string input, double seconds, string timeZoneHandling = "preserve")
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            var dateTimeObject = ParsePartialDate(input, DateTimeType.Fhir);
            var outputTimeZoneHandling = ParseTimeZone(timeZoneHandling);

            dateTimeObject.AddSeconds(seconds);
            return dateTimeObject.ToFhirString(outputTimeZoneHandling);
        }

        public static string AddHyphensDate(string input, string timeZoneHandling = "preserve")
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            var dateTimeObject = ParsePartialDate(input, DateTimeType.Hl7v2);
            var outputTimeZoneHandling = ParseTimeZone(timeZoneHandling);

            dateTimeObject.ConvertToDate();
            return dateTimeObject.ToFhirString(outputTimeZoneHandling);
        }

        public static string FormatAsDateTime(string input, string timeZoneHandling = "preserve")
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            var dateTimeObject = ParsePartialDate(input, DateTimeType.Hl7v2);
            var outputTimeZoneHandling = ParseTimeZone(timeZoneHandling);

            return dateTimeObject.ToFhirString(outputTimeZoneHandling);
        }

        public static string Now(string input, string format = "yyyy-MM-ddTHH:mm:ss.FFFZ")
        {
            return DateTime.UtcNow.ToString(format);
        }

        public static string FormatAsHl7v2DateTime(string input, string timeZoneHandling = "preserve")
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            var dateTimeObject = ParsePartialDate(input, DateTimeType.Fhir);
            var outputTimeZoneHandling = ParseTimeZone(timeZoneHandling);
            return dateTimeObject.ToHl7v2Date(outputTimeZoneHandling);
        }

        public static object FormatWidthAsPeriod(object input)
        {
            if (input == null)
            {
                return input;
            }
            else if (input is not IDictionary<string, object>)
            {
                return input;
            }
            var obj = (IDictionary<string, object>)input;

            PartialDateTime lowDate;
            PartialDateTime highDate;
            if (obj["high"] != null)
            {
                var highDateObj = (IDictionary<string, object>)obj["high"];
                var highDateStr = (string)highDateObj["value"];
                highDate = ParsePartialDate(highDateStr, DateTimeType.Hl7v2);
                lowDate = AddWidthToDate(highDate, -1, obj["width"] as IDictionary<string, object>);
            }
            else if (obj["low"] != null)
            {
                var lowDateObj = (IDictionary<string, object>)obj["low"];
                var lowDateStr = (string)lowDateObj["value"];
                lowDate = ParsePartialDate(lowDateStr, DateTimeType.Hl7v2);
                highDate = AddWidthToDate(lowDate, 1, obj["width"] as IDictionary<string, object>);
            }
            else
            {
                throw new RenderException(
                    FhirConverterErrorCode.InvalidDateTimeFormat,
                    "No low or high date associated with width");
            }

            var lowDict = new Dictionary<string, object>();
            lowDict.Add("value", lowDate.ToHl7v2Date(TimeZoneHandlingMethod.Preserve));
            var highDict = new Dictionary<string, object>();
            highDict.Add("value", highDate.ToHl7v2Date(TimeZoneHandlingMethod.Preserve));
            var result = new Dictionary<string, object>();
            result.Add("low", lowDict);
            result.Add("high", highDict);
            return result;
        }

        private static PartialDateTime AddWidthToDate(PartialDateTime origDate, int intervalMultiplier, IDictionary<string, object> width)
        {
            var widthUnit = ((string)width["unit"]).ToLower();
            var widthValue = Int32.Parse((string)width["value"]);
            var date = origDate.Copy();

            if (widthUnit.StartsWith("s"))
            {
                return date.AddSeconds(intervalMultiplier * widthValue);
            }
            else if (widthUnit.StartsWith("mi"))
            {
                return date.AddMinutes(intervalMultiplier * widthValue);
            }
            else if (widthUnit.StartsWith("h"))
            {
                return date.AddHours(intervalMultiplier * widthValue);
            }
            else if (widthUnit.StartsWith("d"))
            {
                return date.AddDays(intervalMultiplier * widthValue);
            }
            else if (widthUnit.StartsWith("w"))
            {
                return date.AddDays(intervalMultiplier * widthValue * 7);
            }
            else if (widthUnit.StartsWith("mo"))
            {
                return date.AddMonths(intervalMultiplier * widthValue);
            }
            else if (widthUnit.StartsWith("y"))
            {
                return date.AddYears(intervalMultiplier * widthValue);
            }
            else
            {
                throw new RenderException(
                    FhirConverterErrorCode.InvalidDateTimeFormat,
                    $"Invalid datetime width: {widthUnit}");
            }
        }
        
    }
}
