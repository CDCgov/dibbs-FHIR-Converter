// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
        [GeneratedRegex(@"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$", RegexOptions.IgnoreCase)]
        private static partial Regex UuidRegex();

        [GeneratedRegex(@"^([0-2])((\.0)|(\.[1-9][0-9]*))*$", RegexOptions.IgnoreCase)]
        private static partial Regex OidRegex();

        /// <summary>
        /// Returns a specific property of a coding with mapping file Valueset.json
        /// </summary>
        /// <param name="input">The value to look up in the code mappings</param>
        /// <param name="arguments">At 0: The valueset to search in
        ///     At 1: The property to retrieve the value from in the matching valueset (defaults to "code")
        ///     At 2: Whether to apply the default value if input is nil (defaults to "false")</param>
        /// <param name="context">The current template context</param>
        /// <returns>The matching value from Valueset.json.
        ///     Returns null if input is nil and the third argument is false or nil, or if either of the first two arguments is null or empty.</returns>
        public static ValueTask<FluidValue> GetProperty(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            var mapping = arguments.At(0).ToStringValue();
            var property = arguments.At(1).IsNil() ? "code" : arguments.At(1).ToStringValue();

            // If this argument was not passed, NilValue.ToBooleanValue() will return false
            var applyDefaultIfNull = arguments.At(2).ToBooleanValue();
            if ((input.IsNil() && applyDefaultIfNull == false)
                || string.IsNullOrEmpty(mapping)
                || string.IsNullOrEmpty(property))
            {
                return NilValue.Instance;
            }

            var inputString = input.ToStringValue();
            var originalCode = string.IsNullOrEmpty(inputString) ? string.Empty : inputString;

            var map = (context.GetValue("CodeMapping").ToObjectValue() as CodeMapping)?.Mapping?.GetValueOrDefault(
                mapping,
                null);
            var codeMapping =
                map?.GetValueOrDefault(originalCode, null)
                ?? map?.GetValueOrDefault("__default__", null);
            var result = codeMapping?.GetValueOrDefault(property, null)
                ?? ((property.Equals("code") || property.Equals("display")) ? originalCode : null);

            return result == null ? NilValue.Instance : StringValue.Create(result);
        }

        /// <summary>
        /// Generates an ID based on an input string
        /// </summary>
        /// <param name="input">A string used as a seed to generate the UUID</param>
        /// <param name="arguments">Filter arguments (unused)</param>
        /// <param name="context">The current template context (unused)</param>
        /// <returns>A UUID. Returns nil if input is nil or empty.</returns>
        public static ValueTask<FluidValue> GenerateUUID(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            var inputString = input.ToStringValue();
            if (string.IsNullOrWhiteSpace(inputString))
            {
                return NilValue.Instance;
            }

            var bytes = Encoding.UTF8.GetBytes(inputString);
            var hash = SHA256.HashData(bytes);
            var guid = new byte[16];
            Array.Copy(hash, 0, guid, 0, 16);
            return new StringValue(new Guid(guid).ToString());
        }

        /// <summary>
        /// Prepend UUIDs or OIDs to make them valid URNs.
        /// </summary>
        /// <param name="input">String of the ID</param>
        /// <param name="arguments">Arguments passed into the filter (unused)</param>
        /// <param name="context">The current template context (unused)</param>
        /// <returns>If input string matches the UUID pattern return string prepended with `urn:uuid:`, else if it matches the OIN pattern return the input string prepended with `urn:oid:`. It matches neither return the input string unchanged. </returns>
        public static ValueTask<FluidValue> PrependID(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            var inputString = input.ToStringValue();
            if (string.IsNullOrEmpty(inputString))
            {
                return StringValue.Empty;
            }

            if (UuidRegex().IsMatch(inputString))
            {
                return new StringValue("urn:uuid:" + inputString.ToLower());
            }
            else if (OidRegex().IsMatch(inputString))
            {
                return new StringValue("urn:oid:" + inputString);
            }

            return input;
        }

        /// <summary>
        /// This is used to split an ID root from the ID extension when the root and extensions are both URLs.
        /// </summary>
        /// <param name="input">The extension of the ID. This will become the value of the Identifier</param>
        /// <param name="arguments">The root of the ID. This will become the system of the Identifier</param>
        /// <param name="context">The current template context (unused)</param>
        /// <returns>The extension with the root removed, if the root was a URL prefix. Else return the extension unchanged.</returns>
        public static ValueTask<FluidValue> RemovePrefix(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            var extension = input.ToStringValue();
            var root = arguments.At(0).ToStringValue();

            if (
                !string.IsNullOrEmpty(root)
                && !string.IsNullOrEmpty(extension)
                && extension.StartsWith("http://")
                && extension.StartsWith(root))
            {
                string newValue = extension[root.Length..];

                if (newValue.StartsWith('/'))
                {
                    return new StringValue(newValue[1..]);
                }
            }

            return new StringValue(extension);
        }

        /// <summary>
        /// Formats input number as a decimal with a leading 0 if there would be no value before the decimal point.
        /// Retains the decimal precision of the input number
        /// Returns nil if input is not a number
        /// </summary>
        /// <param name="input">An integer or decimal</param>
        /// <param name="arguments">Filter arguments (unused)</param>
        /// <param name="context">The current template context (unused)</param>
        /// <returns>The input formatted as a string with a leading zero as needed, or nil if input is not a number</returns>
        public static ValueTask<FluidValue> FormatDecimal(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            var inputString = input.ToStringValue();
            decimal value;
            if (decimal.TryParse(inputString, out value))
            {
                string format;
                string[] splitDecimal = inputString.Split('.');
                bool hasDecimalPrecision = splitDecimal.Length > 1;
                if (hasDecimalPrecision)
                {
                    format = "0." + new string('0', splitDecimal[1].Length);
                }
                else
                {
                    format = "0";
                }

                return StringValue.Create(value.ToString(format));
            }
            else
            {
                return NilValue.Instance;
            }
        }
    }
}
