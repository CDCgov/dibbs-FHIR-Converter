// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dibbs.Fhir.Liquid.Converter.Exceptions;
using Dibbs.Fhir.Liquid.Converter.Models;
using Dibbs.Fhir.Liquid.Converter.Utilities;
using Fluid;
using Fluid.Values;

namespace Dibbs.Fhir.Liquid.Converter
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

        private static StringValue ConvertStringToDateTime(string inputString, string inputTzHandling, bool convertToDate = false)
        {
            if (string.IsNullOrEmpty(inputString))
            {
                return StringValue.Empty;
            }

            var timeZoneHandling = string.IsNullOrEmpty(inputTzHandling) ? "preserve" : inputTzHandling;
            var outputTimeZoneHandling = ParseTimeZone(timeZoneHandling);
            var dateTimeObject = ParsePartialDate(inputString, DateTimeType.Hl7v2);

            if (convertToDate)
            {
                dateTimeObject.ConvertToDate();
            }

            return StringValue.Create(dateTimeObject.ToFhirString(outputTimeZoneHandling));
        }

        public static ValueTask<FluidValue> AddHyphensDate(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            var inputString = input.ToStringValue();
            var timeZonehandling = arguments.At(0).ToStringValue();
            return ConvertStringToDateTime(inputString, timeZonehandling, true);
        }

        public static ValueTask<FluidValue> FormatAsDateTime(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            var inputString = input.ToStringValue();
            var timeZonehandling = arguments.At(0).ToStringValue();
            return ConvertStringToDateTime(inputString, timeZonehandling);
        }

        public static ValueTask<FluidValue> Now(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            var format = arguments.At(0).ToStringValue();
            if (string.IsNullOrEmpty(format))
            {
                format = "yyyy-MM-ddTHH:mm:ss.FFFZ";
            }

            return StringValue.Create(DateTime.UtcNow.ToString(format));
        }

        public static ValueTask<FluidValue> FormatWidthAsPeriod(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            if (input.IsNil() || input is not DictionaryValue)
            {
                return input;
            }

            var obj = input as DictionaryValue;
            var width = obj.GetValueAsync("width", context).Result;

            // bail out if no width present
            if (width.IsNil())
            {
                return input;
            }

            PartialDateTime lowDate;
            PartialDateTime highDate;
            var high = obj.GetValueAsync("high", context).Result;
            var low = obj.GetValueAsync("low", context).Result;
            if (!high.IsNil())
            {
                var highDateObj = high as DictionaryValue;
                var highDateStr = highDateObj.GetValueAsync("value", context).Result.ToStringValue();
                highDate = ParsePartialDate(highDateStr, DateTimeType.Hl7v2);
                lowDate = AddWidthToDate(highDate, -1, width as DictionaryValue, context);
            }
            else if (!low.IsNil())
            {
                var lowDateObj = low as DictionaryValue;
                var lowDateStr = lowDateObj.GetValueAsync("value", context).Result.ToStringValue();
                lowDate = ParsePartialDate(lowDateStr, DateTimeType.Hl7v2);
                highDate = AddWidthToDate(lowDate, 1, width as DictionaryValue, context);
            }
            else
            {
                // no low or high to add width to
                return input;
            }

            var lowDict = new Dictionary<string, object>();
            lowDict.Add("value", lowDate.ToHl7v2Date(TimeZoneHandlingMethod.Preserve));
            var highDict = new Dictionary<string, object>();
            highDict.Add("value", highDate.ToHl7v2Date(TimeZoneHandlingMethod.Preserve));
            var result = new Dictionary<string, object>();
            result.Add("low", lowDict);
            result.Add("high", highDict);
            return FluidValue.Create(result, TemplateUtility.TemplateOptions);
        }

        private static PartialDateTime AddWidthToDate(PartialDateTime origDate, int intervalMultiplier, DictionaryValue width, TemplateContext context)
        {
            var unit = width.GetValueAsync("unit", context).Result;
            if (unit.IsNil())
            {
                throw new RenderException(
                    FhirConverterErrorCode.InvalidDateTimeFormat,
                    $"Invalid datetime width: no unit");
            }

            var value = width.GetValueAsync("value", context).Result;
            if (value.IsNil())
            {
                throw new RenderException(
                    FhirConverterErrorCode.InvalidDateTimeFormat,
                    $"Invalid datetime width: no value");
            }

            var widthUnit = unit.ToStringValue().ToLower();
            var widthValue = int.Parse(value.ToStringValue());
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
