// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Health.Fhir.Liquid.Converter.InputProcessors;
using Newtonsoft.Json;

namespace Microsoft.Health.Fhir.Liquid.Converter
{
    /// <summary>
    /// Filters for conversion
    /// </summary>
    public partial class Filters
    {
        public static double ToDouble(string data)
        {
            return double.Parse(data);
        }

        public static char CharAt(string data, int index)
        {
            return data[index];
        }

        public static bool Contains(string parentString, string childString)
        {
            return parentString != null && parentString.Contains(childString);
        }

        public static string EscapeSpecialChars(string data)
        {
            return string.IsNullOrEmpty(data) ? data : SpecialCharProcessor.Escape(data);
        }

        public static string UnescapeSpecialChars(string data)
        {
            return string.IsNullOrEmpty(data) ? data : SpecialCharProcessor.Unescape(data);
        }

        public static List<string> Match(string data, string regexString)
        {
            if (string.IsNullOrEmpty(data))
            {
                return new List<string>();
            }

            var regex = new Regex(regexString);
            return regex.Match(data).Captures.Select(capture => capture.Value).ToList();
        }

        public static string ToJsonString(object data)
        {
            return data == null
                ? null
                : JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.None);
        }

        public static string Gzip(string data)
        {
            using var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(data));
            using var outputStream = new MemoryStream();
            using (var gzipStream = new GZipStream(outputStream, CompressionMode.Compress))
            {
                inputStream.CopyTo(gzipStream);
            }

            return Convert.ToBase64String(outputStream.ToArray());
        }

        public static string GunzipBase64String(string data)
        {
            using var inputStream = new MemoryStream(Convert.FromBase64String(data));
            using var outputStream = new MemoryStream();
            using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
            {
                gzipStream.CopyTo(outputStream);
            }

            return Encoding.UTF8.GetString(outputStream.ToArray());
        }

        public static string Sha1Hash(string data)
        {
#pragma warning disable CA5350
            using var sha1 = new SHA1Managed(); // lgtm[cs/weak-crypto]
#pragma warning restore CA5350
            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(data));
            return string.Concat(hash.Select(b => b.ToString("x2")));
        }

        public static string Base64Encode(string data)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(data));
        }

        public static string Base64Decode(string data)
        {
            var bytes = Convert.FromBase64String(data);
            return Encoding.UTF8.GetString(bytes);
        }

        public static string ToXhtml(string xmlString)
        {
            XNamespace xhtmlNamespace = "http://www.w3.org/1999/xhtml";
            XDocument doc;
            try
            {
                doc = XDocument.Parse(xmlString);

                if (doc.Root.Name.LocalName != "div")
                {
                    doc = new XDocument(new XElement(xhtmlNamespace + "div", doc.Root));
                }
            }
            catch (XmlException)
            {
                doc = XDocument.Parse($"<div xmlns=\"http://www.w3.org/1999/xhtml\">{xmlString}</div>");
            }

            ConvertToNamespace(doc.Root, xhtmlNamespace);

            return doc.ToString(SaveOptions.DisableFormatting);
        }

        private static void ConvertToNamespace(XElement element, XNamespace targetNamespace)
        {
            string name;
            switch (element.Name.LocalName)
            {
                case "paragraph":
                case "content":
                    if (element.Ancestors().Any(a => a.Name.LocalName == "p"))
                    {
                        name = "span";
                    }
                    else
                    {
                        name = "p";
                    }

                    break;
                case "caption":
                    if (element.Parent.Name.LocalName == "li")
                    {
                        name = "p";
                    }
                    else
                    {
                        name = "caption";
                    }

                    break;
                case "list":
                    name = "ul";
                    break;
                case "item":
                    name = "li";
                    break;
                case "footnote":
                    name = "small";
                    break;
                case "linkHtml":
                    name = "a";
                    break;
                default:
                    name = element.Name.LocalName;
                    break;
            }

            // Change the element's name to use the target namespace
            element.Name = targetNamespace + name;

            // Remove all namespace declaration attributes
            element.Attributes()
                .Where(a => a.IsNamespaceDeclaration)
                .Remove();

            // Convert attribute names to local names (removes namespace prefixes)
            var attributesToUpdate = element.Attributes().ToList();
            foreach (var attr in attributesToUpdate)
            {
                if (attr.Name.Namespace != XNamespace.None)
                {
                    element.SetAttributeValue(attr.Name.LocalName, attr.Value);
                    attr.Remove();
                }

                if (attr.Name.LocalName == "ID")
                {
                    element.SetAttributeValue("id", attr.Value);
                    attr.Remove();
                }
                else
                {
                    attr.Remove();
                }
            }

            foreach (var child in element.Elements())
            {
                ConvertToNamespace(child, targetNamespace);
            }
        }
    }
}
