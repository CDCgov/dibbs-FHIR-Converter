using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Fluid;
using Fluid.Values;
using Microsoft.VisualBasic.FileIO;

namespace Dibbs.Fhir.Liquid.Converter
{
    /// <summary>
    /// Filters for conversion
    /// </summary>
    public partial class Filters
    {
        private const string InnerTextByIdCacheKey = "Dibbs.Fhir.Liquid.Converter.Filters.InnerTextByIdCache";

        private static string outDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        private static readonly Dictionary<string, string> LoincDict = CSVMapDictionary(Path.Combine(outDir, @"Loinc.csv"));
        private static readonly Dictionary<string, string> SnomedDict = CSVMapDictionary(Path.Combine(outDir, @"Snomed.csv"));
        private static readonly Dictionary<string, string> RxnormDict = CSVMapDictionary(Path.Combine(outDir, @"rxnorm.csv"));
        private static readonly XmlReaderSettings InnerTextByIdReaderSettings = new ()
        {
            DtdProcessing = DtdProcessing.Ignore,
            IgnoreWhitespace = true,
            IgnoreComments = true,
        };

        [GeneratedRegex("[ ]{2,}")]
        private static partial Regex MultispaceRegex();

        /// <summary>
        /// Replaces tabs in input string with spaces
        /// </summary>
        /// <param name="input">A string</param>
        /// <param name="arguments">Filter arguments (unused)</param>
        /// <param name="context">The current template context (unused)</param>
        /// <returns>Nil if input is nil. Otherwise returns the input string with tabs replaced with spaces.</returns>
        public static ValueTask<FluidValue> CleanStringFromTabs(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            if (input.IsNil())
            {
                return NilValue.Instance;
            }

            return StringValue.Create(MultispaceRegex().Replace(input.ToStringValue().Replace("\t", " "), " "));
        }

        /// <summary>
        /// Prints JSON representation of input to console. (Note: "DEBUG_LOG" environment variable must be set to "true")
        /// </summary>
        /// <param name="input">A FluidValue</param>
        /// <param name="arguments">Filter arguments (unused when they are passed into the JSON filter)</param>
        /// <param name="context">The current template context</param>
        /// <returns>Nil</returns>
        public static async ValueTask<FluidValue> PrintObject(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            var debugLog = Environment.GetEnvironmentVariable("DEBUG_LOG") ?? "false";
            if (debugLog.Trim() != "true")
            {
                return NilValue.Instance;
            }

            var json = await Fluid.Filters.MiscFilters.Json(input, arguments, context);
            Console.WriteLine(json.ToStringValue());
            return NilValue.Instance;
        }

        /// <summary>
        /// Parses a CSV file containing keys and values in the first and second columns and returns a dictionary.
        /// </summary>
        /// <returns>A dictionary where the keys are codes and the values are descriptions.</returns>
        private static Dictionary<string, string> CSVMapDictionary(string filename)
        {
            TextFieldParser parser = new (filename);
            Dictionary<string, string> csvData = new ();

            parser.HasFieldsEnclosedInQuotes = true;
            parser.SetDelimiters(",");

            string[] fields;

            while (!parser.EndOfData)
            {
                fields = parser.ReadFields();
                if (fields != null)
                {
                    string key = fields[0].Trim();
                    string value = fields[1].Trim();
                    csvData[key] = value;
                }
            }

            return csvData;
        }

        private static FluidValue GetTerminology(string? code, Dictionary<string, string>? terminologyDict)
        {
            code = code?.Trim();
            terminologyDict.TryGetValue(code ?? string.Empty, out string? element);
            return string.IsNullOrEmpty(element) ? NilValue.Instance : StringValue.Create(element);
        }

        /// <summary>
        /// Retrieves the name associated with the specified LOINC code from the LOINC dictionary.
        /// </summary>
        /// <param name="input">Contains the LOINC code for which to retrieve the name.</param>
        /// <param name="arguments">Arguments passed into the filter (unused).</param>
        /// <param name="context">The current template context (unused).</param>
        /// <returns>The name associated with the specified LOINC code, or null if the code is not found in the dictionary.</returns>
        public static ValueTask<FluidValue> GetLoincName(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            return GetTerminology(input.ToStringValue(), LoincDict);
        }

        /// <summary>
        /// Retrieves the name associated with the specified Snomed code from the Snomed dictionary.
        /// </summary>
        /// <param name="input">Contains the Snomed code for which to retrieve the name.</param>
        /// <param name="arguments">Arguments passed into the filter (unused).</param>
        /// <param name="context">The current template context (unused).</param>
        /// <returns>The name associated with the specified Snomed code, or null if the code is not found in the dictionary.</returns>
        public static ValueTask<FluidValue> GetSnomedName(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            return GetTerminology(input.ToStringValue(), SnomedDict);
        }

        /// <summary>
        /// Retrieves the name associated with the specified RxNorm code from the RxNorm dictionary.
        /// </summary>
        /// <param name="input">Contains the RxNorm code for which to retrieve the name.</param>
        /// <param name="arguments">Arguments passed into the filter (unused).</param>
        /// <param name="context">The current template context (unused).</param>
        /// <returns>The name associated with the specified RxNorm code, or null if the code is not found in the dictionary.</returns>
        public static ValueTask<FluidValue> GetRxnormName(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            return GetTerminology(input.ToStringValue(), RxnormDict);
        }

        /// <summary>
        /// Searches for the original text content of a node with a specified ID within a
        /// given xml string (`text._innerText` in most cases).
        /// </summary>
        /// <param name="input">The string of XML to search within.</param>
        /// <param name="arguments">The ID (reference value) to search for within the data structure.</param>
        /// <param name="context">The current template context.</param>
        /// <returns>A string with the content of the node with the specified ID, or nil if not found.</returns>
        public static ValueTask<FluidValue> FindInnerTextById(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            var id = arguments.At(0).ToStringValue();
            var xml = input.ToStringValue();

            if (string.IsNullOrEmpty(id))
            {
                return NilValue.Instance;
            }

            var result = FindInnerTextById(xml, id, GetInnerTextByIdCache(context));

            return result == null ? NilValue.Instance : StringValue.Create(result);
        }

        private static string? FindInnerTextById(string xml, string id, Dictionary<string, Dictionary<string, XmlElement>> cache)
        {
            if (cache.TryGetValue(xml, out var cachedElementsById))
            {
                return cachedElementsById.TryGetValue(id, out var cachedElement) ? cachedElement.InnerXml : null;
            }

            var elementsById = CreateInnerTextByIdIndex(xml);
            cache[xml] = elementsById;

            return elementsById.TryGetValue(id, out var element) ? element.InnerXml : null;
        }

        // Returns inner text cache from context, creating it if necessary
        private static Dictionary<string, Dictionary<string, XmlElement>> GetInnerTextByIdCache(TemplateContext context)
        {
            if (context.AmbientValues.TryGetValue(InnerTextByIdCacheKey, out var existingCache)
                && existingCache is Dictionary<string, Dictionary<string, XmlElement>> innerTextByIdCache)
            {
                return innerTextByIdCache;
            }

            var cache = new Dictionary<string, Dictionary<string, XmlElement>>(StringComparer.Ordinal);
            context.AmbientValues[InnerTextByIdCacheKey] = cache;

            return cache;
        }

        private static Dictionary<string, XmlElement> CreateInnerTextByIdIndex(string xml)
        {
            var document = new XmlDocument();
            using var stringReader = new StringReader($"<doc>{xml}</doc>");
            using var reader = XmlReader.Create(stringReader, InnerTextByIdReaderSettings);

            document.Load(reader);

            var elementsById = new Dictionary<string, XmlElement>(StringComparer.Ordinal);
            AddElementsById(document.DocumentElement, elementsById);

            return elementsById;
        }

        private static void AddElementsById(XmlNode? node, Dictionary<string, XmlElement> elementsById)
        {
            if (node == null)
            {
                return;
            }

            if (node is XmlElement element)
            {
                var id = GetIdAttribute(element);
                if (id != null && !elementsById.ContainsKey(id))
                {
                    elementsById.Add(id, element);
                }
            }

            foreach (XmlNode child in node.ChildNodes)
            {
                AddElementsById(child, elementsById);
            }
        }

        private static string? GetIdAttribute(XmlElement element)
        {
            foreach (XmlAttribute attribute in element.Attributes)
            {
                if (string.Equals(attribute.LocalName, "id", StringComparison.OrdinalIgnoreCase))
                {
                    return attribute.Value;
                }
            }

            return null;
        }

        /// <summary>
        /// Formats quantity into valid json number.
        /// </summary>
        /// <param name="input">The input data to process, which is a number formatted as a string.</param>
        /// <param name="arguments">Filter arguments (unused)</param>
        /// <param name="context">The current template context (unused)</param>
        /// <returns>A number formatted as a string, with a leading 0 if it's a decimal, and up to 3 decimal places.</returns>
        public static ValueTask<FluidValue> FormatQuantity(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            IConvertible convert = input.ToStringValue();
            return StringValue.Create(convert.ToDouble(null).ToString("0.###"));
        }

        /// <summary>
        /// Searches for an object with a specified ID within a given data structure.
        /// </summary>
        /// <param name="input">The data structure to search within, of type DictionaryValue or ArrayValue.</param>
        /// <param name="arguments">The ID (reference value) to search for within the data structure.</param>
        /// <param name="context">The template context</param>
        /// <returns>A DictionaryValue representing the found object with the specified ID, or nil if not found.</returns>
        public static async ValueTask<FluidValue> FindObjectById(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            if (arguments.At(0).IsNil())
            {
                return NilValue.Instance;
            }

            return await FindObjectByIdRecursive(input, arguments.At(0).ToStringValue(), context);
        }

        /// <summary>
        /// Recursively searches for an object with a specified ID within a given data structure.
        /// </summary>
        /// <param name="data">The data structure to search within, of type IDictionary<string, object>, IList, or JArray.</param>
        /// <param name="id">The ID to search for within the data structure.</param>
        /// <param name="context">The template context</param>
        /// <returns>A DictionaryValue representing the found object with the specified ID, or nil if not found.</returns>
        private static async Task<FluidValue> FindObjectByIdRecursive(FluidValue data, string id, TemplateContext context)
        {
            if (data.IsNil())
            {
                return NilValue.Instance;
            }

            if (data is DictionaryValue dict)
            {
                if ((await dict.GetValueAsync("ID", context)).ToStringValue() == id)
                {
                    return dict;
                }

                foreach (var entry in dict.Enumerate(context))
                {
                    // In Fluid, the enumerated DictionaryValue entries are ArrayValues with the format [StringValue Key, FluidValue Value]
                    var value = await (entry as ArrayValue).GetIndexAsync(NumberValue.Create(1), context);
                    var found = await FindObjectByIdRecursive(value, id, context);
                    if (!found.IsNil())
                    {
                        return found;
                    }
                }
            }
            else if (data is ArrayValue array)
            {
                foreach (var item in array.Enumerate(context))
                {
                    var found = await FindObjectByIdRecursive(item, id, context);
                    if (!found.IsNil())
                    {
                        return found;
                    }
                }
            }

            return NilValue.Instance;
        }
    }
}
