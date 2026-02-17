// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dibbs.Fhir.Liquid.Converter.Exceptions;
using Dibbs.Fhir.Liquid.Converter.Models;
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

        private static FluidValue ConvertStringToDateTime(FluidValue input, string inputTzHandling, bool convertToDate = false)
        {
            var inputString = input.ToStringValue();
            if (string.IsNullOrEmpty(inputString))
            {
                return input;
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

        /// <summary>
        /// Adds hyphens to a date or a partial date that does not have hyphens to make it into a valid FHIR format.
        /// The input date format is YYYY, YYYYMM, or YYYYMMDD.
        /// The output format is a valid FHIR date or a partial date format: YYYY, YYYY-MM, or YYYY-MM-DD.
        /// </summary>
        /// <param name="input">A date string</param>
        /// <param name="arguments">The timezone handling method</param>
        /// <param name="context">The current template context (unused)</param>
        /// <returns>The input date with hyphens added</returns>
        public static ValueTask<FluidValue> AddHyphensDate(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            var timeZonehandling = arguments.At(0).ToStringValue();
            return ConvertStringToDateTime(input, timeZonehandling, true);
        }

        /// <summary>
        /// Converts valid C-CDA datetime to a valid FHIR datetime format.
        /// The input datetime format is datetime or partial datetime without hyphens: YYYY[MM[DD[HH[MM[SS[.S[S[S[S]]]]]]]]][+/-ZZZZ].
        /// For example, the input 20040629175400000 will have the output 2004-06-29T17:54:00.000Z.
        /// Provides parameters to handle different time zones: preserve, utc, local. The default method is preserve.
        /// </summary>
        /// <param name="input">A date string</param>
        /// <param name="arguments">The timezone handling method</param>
        /// <param name="context">The current template context (unused)</param>
        /// <returns>The input date in a valid FHIR datetime format</returns>
        public static ValueTask<FluidValue> FormatAsDateTime(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            var timeZonehandling = arguments.At(0).ToStringValue();
            return ConvertStringToDateTime(input, timeZonehandling);
        }

        /// <summary>
        /// Provides the current time in a specific format.
        /// The default format is yyyy-MM-ddTHH:mm:ss.FFFZ.
        /// </summary>
        /// <param name="input">Unused</param>
        /// <param name="arguments">(Optional) The desired datetime format</param>
        /// <param name="context">The current template context (unused)</param>
        /// <returns>The current datetime</returns>
        public static ValueTask<FluidValue> Now(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            var format = arguments.At(0).ToStringValue();
            if (string.IsNullOrEmpty(format))
            {
                format = "yyyy-MM-ddTHH:mm:ss.FFFZ";
            }

            return StringValue.Create(DateTime.UtcNow.ToString(format));
        }

        /// <summary>
        /// Creates a low and high value from either a low or high value plus a width.
        /// </summary>
        /// <param name="input">An element containing a width element and a high or low element</param>
        /// <param name="arguments">Filter arguments (unused)</param>
        /// <param name="context">The current template context</param>
        /// <returns>A dictionary with a low and high value. Returns nil if input is nil or not a dictionary.</returns>
        public static async ValueTask<FluidValue> FormatWidthAsPeriod(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            if (input.IsNil() || input is not DictionaryValue)
            {
                return input;
            }

            var obj = input as DictionaryValue;
            var width = await obj.GetValueAsync("width", context);

            // bail out if no width present
            if (width.IsNil())
            {
                return input;
            }

            PartialDateTime lowDate;
            PartialDateTime highDate;
            var high = await obj.GetValueAsync("high", context);
            var low = await obj.GetValueAsync("low", context);
            if (!high.IsNil())
            {
                var highDateObj = high as DictionaryValue;
                var highDateStr = (await highDateObj.GetValueAsync("value", context)).ToStringValue();
                highDate = ParsePartialDate(highDateStr, DateTimeType.Hl7v2);
                lowDate = await AddWidthToDate(highDate, -1, width as DictionaryValue, context);
            }
            else if (!low.IsNil())
            {
                var lowDateObj = low as DictionaryValue;
                var lowDateStr = (await lowDateObj.GetValueAsync("value", context)).ToStringValue();
                lowDate = ParsePartialDate(lowDateStr, DateTimeType.Hl7v2);
                highDate = await AddWidthToDate(lowDate, 1, width as DictionaryValue, context);
            }
            else
            {
                // no low or high to add width to
                return input;
            }

            var lowDict = new Dictionary<string, object>
            {
             { "value", lowDate.ToHl7v2Date(TimeZoneHandlingMethod.Preserve) },
            };
            var highDict = new Dictionary<string, object>
            {
             { "value", highDate.ToHl7v2Date(TimeZoneHandlingMethod.Preserve) },
            };
            var result = new Dictionary<string, object>
            {
             { "low", lowDict },
             { "high", highDict },
            };
            return FluidValue.Create(result, context.Options);
        }

        private static async Task<PartialDateTime> AddWidthToDate(PartialDateTime origDate, int intervalMultiplier, DictionaryValue width, TemplateContext context)
        {
            var unit = await width.GetValueAsync("unit", context);
            if (unit.IsNil())
            {
                throw new RenderException(
                    FhirConverterErrorCode.InvalidDateTimeFormat,
                    $"Invalid datetime width: no unit");
            }

            var value = await width.GetValueAsync("value", context);
            if (value.IsNil())
            {
                throw new RenderException(
                    FhirConverterErrorCode.InvalidDateTimeFormat,
                    $"Invalid datetime width: no value");
            }

            var widthUnit = unit.ToStringValue().ToLower();
            var widthValue = int.Parse(value.ToStringValue());
            var date = origDate.Copy();

            if (widthUnit.StartsWith('s'))
                {
                    return date.AddSeconds(intervalMultiplier * widthValue);
                }
                else if (widthUnit.StartsWith("mi"))
                {
                    return date.AddMinutes(intervalMultiplier * widthValue);
                }
                else if (widthUnit.StartsWith('h'))
                {
                    return date.AddHours(intervalMultiplier * widthValue);
                }
                else if (widthUnit.StartsWith('d'))
                {
                    return date.AddDays(intervalMultiplier * widthValue);
                }
                else if (widthUnit.StartsWith('w'))
                {
                    return date.AddDays(intervalMultiplier * widthValue * 7);
                }
                else if (widthUnit.StartsWith("mo"))
                {
                    return date.AddMonths(intervalMultiplier * widthValue);
                }
                else if (widthUnit.StartsWith('y'))
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
