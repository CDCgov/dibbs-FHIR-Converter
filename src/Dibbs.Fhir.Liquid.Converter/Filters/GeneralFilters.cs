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

            return result == null ? NilValue.Instance : new StringValue(result);
        }

        public static ValueTask<FluidValue> GenerateUUID(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            var inputString = input.ToStringValue();
            if (string.IsNullOrWhiteSpace(inputString))
            {
                return StringValue.Empty;
            }

            var bytes = Encoding.UTF8.GetBytes(inputString);
            var algorithm = SHA256.Create();
            var hash = algorithm.ComputeHash(bytes);
            var guid = new byte[16];
            Array.Copy(hash, 0, guid, 0, 16);
            return new StringValue(new Guid(guid).ToString());
        }

        /// <summary>
        /// Prepend UUIDs or OIDs to make them valid URNs.
        /// </summary>
        /// <param name="input">String of the ID</param>
        /// /// <param name="arguments">Arguments passed into the filter</param>
        /// /// <param name="context">The template context</param>
        /// <returns>If input string matches the UUID pattern return string prepended with `urn:uuid:`, else if it matches the OIN pattern return the input string prepended with `urn:oid:`. It matches neither return the input string unchanged. </returns>
        public static ValueTask<FluidValue> PrependID(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            var inputString = input.ToStringValue();
            if (string.IsNullOrEmpty(inputString))
            {
                return StringValue.Empty;
            }

            string uuid_pattern = @"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$";
            string oid_pattern = @"^([0-2])((\.0)|(\.[1-9][0-9]*))*$";

            if (Regex.IsMatch(inputString, uuid_pattern, RegexOptions.IgnoreCase))
            {
                return new StringValue("urn:uuid:" + inputString.ToLower());
            }
            else if (Regex.IsMatch(inputString, oid_pattern, RegexOptions.IgnoreCase))
            {
                return new StringValue("urn:oid:" + inputString);
            }

            return input;
        }

        /// <summary>
        /// This is used to split an ID root from the ID extension when the root and extensions are both URLs.
        /// </summary>
        /// <param name="extension">The extension of the ID. This will become the value of the Identifier</param>
        /// <param name="root">The root of the ID. This will become the system of the Identifier</param>
        /// <returns>The extension with the root removed, if the root was a URL prefix. Else return the extension unchanged.</returns>
        public static ValueTask<FluidValue> RemovePrefix(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            if (input.IsNil())
            {
                return StringValue.Empty;
            }

            var extension = input.ToStringValue();
            var root = arguments.At(0).ToStringValue();

            if (
                string.IsNullOrEmpty(root)
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
    }
}
