// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Dibbs.Fhir.Liquid.Converter.InputProcessors;
using Fluid;
using Fluid.Values;

namespace Dibbs.Fhir.Liquid.Converter
{
    /// <summary>
    /// Filters for conversion
    /// </summary>
    public partial class Filters
    {
        public static ValueTask<FluidValue> EscapeSpecialChars(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            var data = input.ToStringValue();
            var result = string.IsNullOrEmpty(data) ? data : SpecialCharProcessor.Escape(data);
            return new StringValue(result);
        }

        public static ValueTask<FluidValue> Match(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            var inputString = input.ToStringValue();
            if (string.IsNullOrEmpty(inputString))
            {
                return ArrayValue.Empty;
            }

            var regex = new Regex(arguments.At(0).ToStringValue());
            var matches = regex.Match(inputString).Captures.Select(capture => new StringValue(capture.Value)).ToList();
            return new ArrayValue(matches);
        }

        // Overriding Fluid's prepend filter to mimic the behavior of older versions of DotLiquid
        public static ValueTask<FluidValue> Prepend(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            if (input.IsNil() || string.IsNullOrEmpty(input.ToStringValue()))
            {
                return NilValue.Instance;
            }

            return Fluid.Filters.StringFilters.Prepend(input, arguments, context);
        }

        // Overriding Fluid's append filter to mimic the behavior of older versions of DotLiquid
        public static ValueTask<FluidValue> Append(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            if (input.IsNil())
            {
                return NilValue.Instance;
            }

            return Fluid.Filters.StringFilters.Append(input, arguments, context);
        }

        public static ValueTask<FluidValue> ToJsonString(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            if (input.IsNil())
            {
                return NilValue.Instance;
            }

            return Fluid.Filters.MiscFilters.Json(input, arguments, context);
        }

        public static ValueTask<FluidValue> Gzip(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            var data = input.ToStringValue();
            if (string.IsNullOrEmpty(data))
            {
                return StringValue.Empty;
            }

            using var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(data));
            using var outputStream = new MemoryStream();
            using (var gzipStream = new GZipStream(outputStream, CompressionMode.Compress))
            {
                inputStream.CopyTo(gzipStream);
            }

            return StringValue.Create(Convert.ToBase64String(outputStream.ToArray()));
        }

        public static ValueTask<FluidValue> ToXhtml(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            XNamespace xhtmlNamespace = "http://www.w3.org/1999/xhtml";
            XDocument doc;
            var xmlString = input.ToStringValue();
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

            return new StringValue(doc.ToString(SaveOptions.DisableFormatting));
        }

        public static ValueTask<FluidValue> RemoveRegex(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            if (arguments.Count != 1)
            {
                throw new ArgumentException("RemoveRegex requires one argument.");
            }

            string result = Regex.Replace(input.ToStringValue(), arguments.At(0).ToStringValue(), string.Empty);
            return new StringValue(result);
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
