// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using DotLiquid;
using Microsoft.Health.Fhir.Liquid.Converter.Exceptions;
using Microsoft.Health.Fhir.Liquid.Converter.Models;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Fhir.Liquid.Converter
{
    /// <summary>
    /// Filters for conversion
    /// </summary>
    public partial class Filters
    {
        public static string GetProperty(
            Context context,
            string originalCode,
            string mapping,
            string property = "code")
        {
            if (
                string.IsNullOrEmpty(originalCode)
                || string.IsNullOrEmpty(mapping)
                || string.IsNullOrEmpty(property))
            {
                return null;
            }

            var map = (context["CodeMapping"] as CodeMapping)?.Mapping?.GetValueOrDefault(
                mapping,
                null);
            var codeMapping =
                map?.GetValueOrDefault(originalCode, null)
                ?? map?.GetValueOrDefault("__default__", null);
            return codeMapping?.GetValueOrDefault(property, null)
                ?? ((property.Equals("code") || property.Equals("display")) ? originalCode : null);
        }

        public static string Evaluate(string input, string property)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(property))
            {
                return null;
            }

            var obj = JObject.Parse(input);
            var memberToken = obj.SelectToken(property);
            return memberToken?.Value<string>();
        }

        public static string GetObject(string input, string path)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(path))
            {
                return null;
            }

            var obj = JObject.Parse(input);
            var memberToken = obj.SelectToken(path);

            return memberToken.ToString();
        }

        public static string GenerateIdInput(
            string segment,
            string resourceType,
            bool isBaseIdRequired,
            string baseId = null)
        {
            if (string.IsNullOrWhiteSpace(segment))
            {
                return null;
            }

            if (
                string.IsNullOrEmpty(resourceType)
                || (isBaseIdRequired && string.IsNullOrEmpty(baseId)))
            {
                throw new RenderException(
                    FhirConverterErrorCode.InvalidIdGenerationInput,
                    Resources.InvalidIdGenerationInput);
            }

            segment = segment.Trim();
            return baseId != null
                ? $"{resourceType}_{segment}_{baseId}"
                : $"{resourceType}_{segment}";
        }

        public static string GenerateUUID(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            var bytes = Encoding.UTF8.GetBytes(input);
            var algorithm = SHA256.Create();
            var hash = algorithm.ComputeHash(bytes);
            var guid = new byte[16];
            Array.Copy(hash, 0, guid, 0, 16);
            return new Guid(guid).ToString();
        }

        /// <summary>
        /// Prepend UUIDs or OIDs to make them valid URNs.
        /// </summary>
        /// <param name="input">String of the ID</param>
        /// <returns>If input string matches the UUID pattern return string prepended with `urn:uuid:`, else if it matches the OIN pattern return the input string prepended with `urn:oid:`. It matches neither return the input string unchanged. </returns>
        public static string PrependID(string input)
        {
            if (input == null)
            {
                return input;
            }

            string uuid_pattern = @"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$";
            string oid_pattern = @"^([0-2])((\.0)|(\.[1-9][0-9]*))*$";
            string output = "urn:";

            if (Regex.IsMatch(input, uuid_pattern, RegexOptions.IgnoreCase))
            {
                output = output + "uuid:" + input.ToLower();
            }
            else if (Regex.IsMatch(input, oid_pattern, RegexOptions.IgnoreCase))
            {
                output = output + "oid:" + input;
            }
            else
            {
                output = input;
            }

            return output;
        }

        /// <summary>
        /// This is used to split an ID root from the ID extension when the root and extensions are both URLs.
        /// </summary>
        /// <param name="extension">The extension of the ID. This will become the value of the Identifier</param>
        /// <param name="root">The root of the ID. This will become the system of the Identifier</param>
        /// <returns>The extension with the root removed, if the root was a URL prefix. Else return the extension unchanged.</returns>
        public static string RemovePrefix(string extension, string root)
        {
            // We potentially have to strip quote marks from the root because the output of the SystemReference template includes the quote marks.
            extension = extension.StartsWith('"') ? extension[1..] : extension;
            extension = extension.EndsWith('"') ? extension[..^1] : extension;

            root = root.StartsWith('"') ? root[1..] : root;
            root = root.EndsWith('"') ? root[..^1] : root;

            var httpPrefix = "http://";
            if (
                root != null
                && extension != null
                && extension.StartsWith(httpPrefix)
                && extension.StartsWith(root))
            {
                string newValue = extension[root.Length..];

                if (newValue.StartsWith('/'))
                {
                    return '"' + newValue[1..] + '"';
                }
            }

            return '"' + extension + '"';
        }
    }
}
