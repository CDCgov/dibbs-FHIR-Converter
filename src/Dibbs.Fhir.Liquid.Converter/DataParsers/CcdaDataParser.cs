// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Dibbs.Fhir.Liquid.Converter.Exceptions;
using Dibbs.Fhir.Liquid.Converter.Models;

namespace Dibbs.Fhir.Liquid.Converter.DataParsers
{
    public class CcdaDataParser : IDataParser
    {
        public object Parse(string document)
        {
            if (string.IsNullOrWhiteSpace(document))
            {
                throw new DataParseException(FhirConverterErrorCode.NullOrWhiteSpaceInput, Resources.NullOrWhiteSpaceInput);
            }

            try
            {
                var xDocument = XDocument.Parse(document);

                // Serialize contents of `text` elements as string in `_innerText` child element
                StringifyTextNodeContents(xDocument?.Root);

                // Remove redundant namespaces to avoid appending namespace prefix before elements
                var defaultNamespace = xDocument.Root?.GetDefaultNamespace().NamespaceName;
                xDocument.Root?.Attributes()
                    .Where(attribute => IsRedundantNamespaceAttribute(attribute, defaultNamespace))
                    .Remove();

                // Normalize non-default namespace prefix in elements
                var namespaces = xDocument.Root?.Attributes()
                    .Where(x => x.IsNamespaceDeclaration && x.Value != defaultNamespace);
                NormalizeNamespacePrefix(xDocument?.Root, namespaces);

                // Change XText to XElement with name "_" to avoid serializing depth difference, e.g., given="foo" and given.#text="foo"
                ReplaceTextWithElement(xDocument?.Root);

                // Convert to dictionary
                var dataDictionary = ConvertToDictionary(xDocument);

                // Remove line breaks in original data
                // string.Replace is faster and uses less memory than regex.Replace
                dataDictionary["_originalData"] = document
                    .Replace("\r\n", string.Empty)
                    .Replace("\n", string.Empty)
                    .Replace("\r", string.Empty);

                return dataDictionary;
            }
            catch (Exception ex)
            {
                throw new DataParseException(FhirConverterErrorCode.InputParsingError, string.Format(Resources.InputParsingError, ex.Message), ex);
            }
        }

        private static Dictionary<string, object> ConvertToDictionary(XDocument document)
        {
            return new Dictionary<string, object>
            {
                [document.Root.Name.LocalName] = ConvertElement(document.Root),
            };
        }

        private static object ConvertElement(XElement element)
        {
            var value = new Dictionary<string, object>();

            foreach (var attribute in element.Attributes())
            {
                value[GetAttributeName(attribute)] = CleanStringValue(attribute.Value);
            }

            var childNodeValues = new Dictionary<string, List<object>>();
            foreach (var childNode in element.Nodes())
            {
                switch (childNode)
                {
                    case XElement childElement:
                        AddChildNodeValue(childNodeValues, childElement.Name.LocalName, ConvertElement(childElement));
                        break;
                    case XComment comment:
                        AddChildNodeValue(childNodeValues, "#comment", CleanStringValue(comment.Value));
                        break;
                    case XProcessingInstruction processingInstruction:
                        AddChildNodeValue(childNodeValues, $"?{processingInstruction.Target}", CleanStringValue(processingInstruction.Data));
                        break;
                }
            }

            foreach (var childNodeValue in childNodeValues)
            {
                value[childNodeValue.Key] = childNodeValue.Value.Count == 1 ? childNodeValue.Value[0] : childNodeValue.Value;
            }

            var textNodes = element.Nodes().OfType<XText>().ToList();
            if (textNodes.Count > 0)
            {
                var textValue = CleanStringValue(string.Concat(textNodes.Select(textNode => textNode.Value)));
                if (value.Count == 0)
                {
                    return textValue;
                }

                value["#text"] = textValue;
            }

            return value.Count > 0 ? value : null;
        }

        private static void AddChildNodeValue(IDictionary<string, List<object>> childNodeValues, string name, object value)
        {
            if (!childNodeValues.TryGetValue(name, out var values))
            {
                values = new List<object>();
                childNodeValues[name] = values;
            }

            values.Add(value);
        }

        private static string GetAttributeName(XAttribute attribute)
        {
            if (attribute.IsNamespaceDeclaration)
            {
                return string.Equals(attribute.Name.LocalName, "xmlns", StringComparison.Ordinal)
                    ? "xmlns"
                    : $"xmlns:{attribute.Name.LocalName}";
            }

            var attributeNamespace = attribute.Name.Namespace;
            if (attributeNamespace == XNamespace.None)
            {
                return attribute.Name.LocalName;
            }

            var prefix = attribute.Parent?.GetPrefixOfNamespace(attributeNamespace);
            return string.IsNullOrEmpty(prefix)
                ? attribute.Name.LocalName
                : $"{prefix}:{attribute.Name.LocalName}";
        }

        private static string CleanStringValue(string value)
        {
            return CcdaDataParserRegex.NewlineRegex().Replace(value, string.Empty);
        }

        private static bool IsRedundantNamespaceAttribute(XAttribute attribute, string defaultNamespace)
        {
            return attribute != null &&
                   attribute.IsNamespaceDeclaration &&
                   !string.Equals(attribute.Name.LocalName, "xmlns", StringComparison.InvariantCultureIgnoreCase) &&
                   string.Equals(attribute.Value, defaultNamespace, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Replace "namespace:attribute" to "namespace_attribute" to be compatible with DotLiquids, e.g., from sdtc:raceCode to sdtc_raceCode
        /// </summary>
        private static void NormalizeNamespacePrefix(XElement element, IEnumerable<XAttribute> namespaces)
        {
            if (element == null || namespaces == null)
            {
                return;
            }

            foreach (var ns in namespaces)
            {
                if (string.Equals(ns.Value, element.Name.NamespaceName, StringComparison.InvariantCultureIgnoreCase))
                {
                    element.Name = $"{ns.Name.LocalName}_{element.Name.LocalName}";
                    break;
                }
            }

            foreach (var childElement in element.Elements())
            {
                NormalizeNamespacePrefix(childElement, namespaces);
            }
        }

        private static void ReplaceTextWithElement(XElement element)
        {
            if (element == null)
            {
                return;
            }

            // Iterate reversely as the list itself is updating
            var nodes = element.Nodes().ToList();
            for (var i = nodes.Count - 1; i >= 0; --i)
            {
                switch (nodes[i])
                {
                    case XText textNode:
                        element.Add(new XElement("_", textNode.Value));
                        textNode.Remove();
                        break;
                    case XElement elementNode:
                        ReplaceTextWithElement(elementNode);
                        break;
                }
            }
        }

        private static void StringifyTextNodeContents(XElement element)
        {
            if (element == null)
            {
                return;
            }

            System.Xml.XmlWriterSettings xws = new ()
            {
                OmitXmlDeclaration = true,
                ConformanceLevel = System.Xml.ConformanceLevel.Fragment,
            };

            foreach (var el in element.Descendants())
            {
                if (el.Name.LocalName == "text")
                {
                    System.Text.StringBuilder sb = new ();

                    using (System.Xml.XmlWriter xw = System.Xml.XmlWriter.Create(sb, xws))
                    {
                        foreach (var node in el.Nodes())
                        {
                            node.WriteTo(xw);
                        }
                    }

                    var content = sb.ToString();
                    el.SetAttributeValue("_innerText", content);
                }
            }
        }
    }
}
