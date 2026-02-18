// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Fluid;
using Microsoft.Health.Fhir.Liquid.Converter.DotLiquids;
using Microsoft.Health.Fhir.Liquid.Converter.Exceptions;
using Microsoft.Health.Fhir.Liquid.Converter.Models;
using Microsoft.Health.Fhir.Liquid.Converter.Models.Json;
using Microsoft.Health.Fhir.Liquid.Converter.Tags;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema;
using NJsonSchema.Validation;

namespace Microsoft.Health.Fhir.Liquid.Converter.Utilities
{
    public static class TemplateUtility
    {
        private static readonly Regex FormatRegex = new Regex(@"(\\|/)_?");
        private const string LiquidTemplateFileExtension = ".liquid";
        private const string JsonSchemaTemplateFileExtension = ".schema.json";
        private const string MetaJsonSchemaFileName = "meta-schema.json";
        private static readonly JsonSchema MetaJsonSchema;

        // We only use one instance of TemplateOptions because it caches templates that are referenced by include or render tags and we can re-use them for other eCRs
        private static readonly TemplateOptions TemplateOptionsValue;

        // Instantiating a FluidParser instance is expensive
        // so this is the one we will use across the whole application
        public static readonly FluidParser Parser;

        // Register "evaluate" tag in before Template.Parse
        static TemplateUtility()
        {
            // Template.RegisterTag<MergeDiff>("mergeDiff");
            // Template.RegisterTag<Validate>("validate");
            MetaJsonSchema = LoadEmbeddedMetaJsonSchema();

            // AllowParentheses allows grouping of expressions in templates with parentheses
            Parser = new FluidParser(new FluidParserOptions { AllowParentheses = true });
            Parser.RegisterParserTag("evaluate", EvaluateParser.Parser, async (evaluateTag, w, e, c) =>
            {
                return await evaluateTag.WriteToAsync(w, e, c);
            });

            TemplateOptionsValue = new TemplateOptions
            {
                MaxSteps = 10000000,
            };
            AddFilters(TemplateOptionsValue);
        }

        public static string RootTemplateParentPathScope => "RootTemplateParentPath";

        public static TemplateOptions TemplateOptions => TemplateOptionsValue;

        /// <summary>
        /// Parse templates from string, "CodeSystem/CodeSystem.json" and "ValueSet/ValueSet.json" are used for Hl7v2 and C-CDA data type code mapping respectively
        /// </summary>
        /// <param name="templates">A dictionary, key is the name, value is the template content in string format</param>
        /// <returns>A dictionary, key is the name, value is Template</returns>
        public static Dictionary<string, IFluidTemplate> ParseTemplates(IDictionary<string, string> templates)
        {
            var parsedTemplates = new Dictionary<string, IFluidTemplate>();
            foreach (var entry in templates)
            {
                var formattedEntryKey = FormatRegex.Replace(entry.Key, "/");

                string templateKey = GetTemplateKey(formattedEntryKey);

                if (templateKey != null)
                {
                    parsedTemplates[templateKey] = ParseTemplate(templateKey, entry.Value);
                }
            }

            return parsedTemplates;
        }

        /// <summary>
        /// Get template key from template file path.
        /// Liquid template keys and code mapping template keys have no suffix extension, like "CodeSystem/CodeSystem", "ValueSet/ValueSet".
        /// Json schema template keys have the suffix ".schema.json".
        /// Will return null if extension of given template file is not supported.
        /// </summary>
        /// <param name="templatePath">A template file path</param>
        /// <returns>A template key</returns>
        public static string GetTemplateKey(string templatePath)
        {
            if (templatePath.Contains("CodeSystem/CodeSystem.json", StringComparison.InvariantCultureIgnoreCase)
                || templatePath.Contains("ValueSet/ValueSet.json", StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(Path.GetExtension(templatePath), LiquidTemplateFileExtension, StringComparison.InvariantCultureIgnoreCase))
            {
                return Path.ChangeExtension(templatePath, null);
            }
            else if (IsJsonSchemaTemplate(templatePath))
            {
                return templatePath;
            }
            else
            {
                return null;
            }
        }

        public static IFluidTemplate ParseTemplate(string templateKey, string content)
        {
            if (IsCodeMappingTemplate(templateKey))
            {
                return ParseCodeMapping(content);
            }
            else if (IsJsonSchemaTemplate(templateKey))
            {
                return ParseJsonSchemaTemplate(content);
            }
            else
            {
                return ParseLiquidTemplate(templateKey, content);
            }
        }

        public static IFluidTemplate ParseCodeMapping(string content)
        {
            if (content == null)
            {
                return null;
            }

            try
            {
                var mapping = JsonConvert.DeserializeObject<CodeMapping>(content);
                if (mapping?.Mapping == null)
                {
                    throw new TemplateLoadException(FhirConverterErrorCode.InvalidCodeMapping, Resources.InvalidCodeMapping);
                }

                var template = Parser.Parse(string.Empty);
                template.Root = new CodeMappingDocument(new List<CodeMapping>() { mapping });
                return template;
            }
            catch (JsonException ex)
            {
                throw new TemplateLoadException(FhirConverterErrorCode.InvalidCodeMapping, Resources.InvalidCodeMapping, ex);
            }
        }

        public static IFluidTemplate ParseLiquidTemplate(string templateName, string content)
        {
            if (content == null)
            {
                return null;
            }

            try
            {
                if (Parser.TryParse(content, out var template, out var error))
                {
                    return template;
                }
                else
                {
                    throw new TemplateLoadException(
                        FhirConverterErrorCode.TemplateSyntaxError,
                        string.Format(Resources.TemplateSyntaxError, templateName, error));
                }
            }
            catch (Exception ex)
            {
                throw new TemplateLoadException(FhirConverterErrorCode.TemplateSyntaxError, string.Format(Resources.TemplateSyntaxError, templateName, ex.Message), ex);
            }
        }

        public static IFluidTemplate ParseJsonSchemaTemplate(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new TemplateLoadException(FhirConverterErrorCode.InvalidJsonSchema, "Schema cannot be null or empty.");
            }

            // Validate input Json schema
            ICollection<ValidationError> errors;
            try
            {
                var schemaObject = JObject.Parse(content);
                errors = MetaJsonSchema.Validate(content);
            }
            catch (Exception ex)
            {
                throw new TemplateLoadException(FhirConverterErrorCode.InvalidJsonSchema, string.Format(Resources.InvalidJsonSchemaContent, ex.Message), ex);
            }

            if (errors.Any())
            {
                throw new TemplateLoadException(FhirConverterErrorCode.InvalidJsonSchema, string.Format(Resources.InvalidJsonSchemaContent, string.Join(";", errors)));
            }

            JsonSchema schema;
            try
            {
                schema = JsonSchema.FromJsonAsync(content).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                throw new TemplateLoadException(FhirConverterErrorCode.InvalidJsonSchema, string.Format(Resources.InvalidJsonSchemaContent, ex.Message), ex);
            }

            var template = Parser.Parse(string.Empty);
            template.Root = new JSchemaDocument(schema);
            return template;
        }

        public static bool IsCodeMappingTemplate(string templateKey)
        {
            return templateKey.EndsWith("CodeSystem/CodeSystem", StringComparison.InvariantCultureIgnoreCase) ||
                   templateKey.EndsWith("ValueSet/ValueSet", StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsJsonSchemaTemplate(string templateKey)
        {
            return templateKey.EndsWith(JsonSchemaTemplateFileExtension, StringComparison.InvariantCultureIgnoreCase);
        }

        private static JsonSchema LoadEmbeddedMetaJsonSchema()
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            var metaSchemaAssemblyName = string.Format("{0}.{1}", executingAssembly.GetName().Name, MetaJsonSchemaFileName);

            string metaSchemaContent;
            using (Stream stream = executingAssembly.GetManifestResourceStream(metaSchemaAssemblyName))
            using (StreamReader reader = new StreamReader(stream))
            {
                metaSchemaContent = reader.ReadToEnd();
            }

            return JsonSchema.FromJsonAsync(metaSchemaContent).GetAwaiter().GetResult();
        }

        public static string GetRootTemplateParentPath(string rootTemplate)
        {
            string[] rootTemplateParts = rootTemplate.Split('/');
            return string.Join("/", rootTemplateParts, 0, rootTemplateParts.Length - 1);
        }

        public static string GetFormattedTemplatePath(string templateName, string rootTemplateParentPath = "")
        {
            return string.IsNullOrEmpty(rootTemplateParentPath) ? templateName : string.Format("{0}/{1}", rootTemplateParentPath, templateName);
        }

        public static void AddFilters(TemplateOptions options)
        {
            // CollectionFilters
            options.Filters.AddFilter("to_array", Filters.ToArray);
            options.Filters.AddFilter("batch_render", Filters.BatchRender);
            options.Filters.AddFilter("nested_where", Filters.NestedWhere);

            // CustomFilters
            options.Filters.AddFilter("clean_string_from_tabs", Filters.CleanStringFromTabs);
            options.Filters.AddFilter("print_object", Filters.PrintObject);
            options.Filters.AddFilter("get_loinc_name", Filters.GetLoincName);
            options.Filters.AddFilter("get_snomed_name", Filters.GetSnomedName);
            options.Filters.AddFilter("get_rxnorm_name", Filters.GetRxnormName);
            options.Filters.AddFilter("find_inner_text_by_id", Filters.FindInnerTextById);
            options.Filters.AddFilter("format_quantity", Filters.FormatQuantity);

            // DateFilters
            options.Filters.AddFilter("add_hyphens_date", Filters.AddHyphensDate);
            options.Filters.AddFilter("format_as_date_time", Filters.FormatAsDateTime);
            options.Filters.AddFilter("now", Filters.Now);
            options.Filters.AddFilter("format_width_as_period", Filters.FormatWidthAsPeriod);

            // GeneralFilters
            options.Filters.AddFilter("prepend_id", Filters.PrependID);
            options.Filters.AddFilter("generate_uuid", Filters.GenerateUUID);
            options.Filters.AddFilter("get_property", Filters.GetProperty);
            options.Filters.AddFilter("remove_prefix", Filters.RemovePrefix);

            // SectionFilters
            options.Filters.AddFilter("get_first_ccda_sections_by_template_id", Filters.GetFirstCcdaSectionsByTemplateId);

            // StringFilters
            options.Filters.AddFilter("remove_regex", Filters.RemoveRegex);
            options.Filters.AddFilter("match", Filters.Match);
            options.Filters.AddFilter("to_xhtml", Filters.ToXhtml);
            options.Filters.AddFilter("escape_special_chars", Filters.EscapeSpecialChars);
            options.Filters.AddFilter("prepend", Filters.Prepend);
            options.Filters.AddFilter("append", Filters.Append);
            options.Filters.AddFilter("to_json_string", Filters.ToJsonString);
            options.Filters.AddFilter("gzip", Filters.Gzip);
        }
    }
}
