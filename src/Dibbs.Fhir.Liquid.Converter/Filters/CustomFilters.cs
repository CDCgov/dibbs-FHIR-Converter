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
        private static string outDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        private static Dictionary<string, string> loincDict = CSVMapDictionary(Path.Combine(outDir, @"Loinc.csv"));
        private static Dictionary<string, string> snomedDict = CSVMapDictionary(Path.Combine(outDir, @"Snomed.csv"));
        private static Dictionary<string, string> rxnormDict = CSVMapDictionary(Path.Combine(outDir, @"rxnorm.csv"));

        public static ValueTask<FluidValue> CleanStringFromTabs(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            if (input.IsNil())
            {
                return NilValue.Instance;
            }

            const string reduceMultiSpace = @"[ ]{2,}";
            return StringValue.Create(Regex.Replace(input.ToStringValue().Replace("\t", " "), reduceMultiSpace, " "));
        }

        public static ValueTask<FluidValue> PrintObject(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            var devMode = Environment.GetEnvironmentVariable("DEV_MODE") ?? "false";
            var debugLog = Environment.GetEnvironmentVariable("DEBUG_LOG") ?? "false";
            if (devMode.Trim() != "true" || debugLog.Trim() != "true")
            {
                return NilValue.Instance;
            }

            Console.WriteLine(Fluid.Filters.MiscFilters.Json(input, arguments, context).Result.ToStringValue());
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
        /// <param name="code">The LOINC code for which to retrieve the name.</param>
        /// <returns>The name associated with the specified LOINC code, or null if the code is not found in the dictionary.</returns>
        public static ValueTask<FluidValue> GetLoincName(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            return GetTerminology(input.ToStringValue(), loincDict);
        }

        /// <summary>
        /// Retrieves the name associated with the specified Snomed code from the LOINC dictionary.
        /// </summary>
        /// <param name="code">The Snomed code for which to retrieve the name.</param>
        /// <returns>The name associated with the specified Snomed code, or null if the code is not found in the dictionary.</returns>
        public static ValueTask<FluidValue> GetSnomedName(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            return GetTerminology(input.ToStringValue(), snomedDict);
        }

        /// <summary>
        /// Retrieves the name associated with the specified RxNorm code from the RxNorm dictionary.
        /// </summary>
        /// <param name="code">The RxNorm code for which to retrieve the name.</param>
        /// <returns>The name associated with the specified RxNorm code, or null if the code is not found in the dictionary.</returns>
        public static ValueTask<FluidValue> GetRxnormName(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            return GetTerminology(input.ToStringValue(), rxnormDict);
        }

        /// <summary>
        /// Searches for the original text content of a node with a specified ID within a
        /// given xml string (`text._innerText` in most cases).
        /// </summary>
        /// <param name="fullText">The string of XML to search within.</param>
        /// <param name="id">The ID (reference value) to search for within the data structure.</param>
        /// <returns>A string with the content of the node with the specified ID, or null if not found.</returns>
        public static ValueTask<FluidValue> FindInnerTextById(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            XmlDocument doc = new ();

            // Add wrapper <doc> as the fragment may not have one root node.
            doc.LoadXml($"<doc>{input.ToStringValue()}</doc>");
            XmlElement root = doc.DocumentElement;
            var result = FindInnerTextByIdRecursive(root, arguments.At(0).ToStringValue());
            return result == null ? NilValue.Instance : StringValue.Create(result);
        }

        private static string? FindInnerTextByIdRecursive(XmlElement root, string id)
        {
            foreach (XmlNode node in root.ChildNodes)
            {
                if (node is XmlElement el)
                {
                    foreach (XmlAttribute attr in el.Attributes)
                    {
                        if (attr.LocalName.ToLower() == "id" && attr.Value == id)
                        {
                            return el.InnerXml;
                        }
                    }

                    var res = FindInnerTextByIdRecursive(el, id);
                    if (res != null)
                    {
                        return res;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Formats quantity into valid json number.
        /// </summary>
        /// <param name="input">The input data to process, which is a number formatted as a string.</param>
        /// <returns>A number formatted as a string, with a leading 0 if it's a decimal, and up to 3 decimal places.</returns>
        public static ValueTask<FluidValue> FormatQuantity(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            IConvertible convert = input.ToStringValue();
            return StringValue.Create(convert.ToDouble(null).ToString("0.###"));
        }
    }
}
