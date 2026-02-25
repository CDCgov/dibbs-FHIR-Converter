// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Text.Json;
using Dibbs.Fhir.Liquid.Converter.Exceptions;
using Dibbs.Fhir.Liquid.Converter.Models;
using Fluid;

namespace Dibbs.Fhir.Liquid.Converter.Utilities
{
    public static class TemplateUtility
    {
        private static readonly string TemplateDirectoryValue = Environment.GetEnvironmentVariable("TEMPLATES_PATH") ?? "../../data/Templates/eCR";

        // We only use one instance of TemplateOptions because it caches templates that are referenced by include or render tags and we can re-use them for other eCRs
        private static readonly TemplateOptions TemplateOptionsValue;

        // Instantiating a FluidParser instance is expensive
        // so this is the one we will use across the whole application
        public static readonly FluidParser Parser;

        static TemplateUtility()
        {
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

        public static string RootTemplate => "EICR";

        public static string TemplateDirectory => TemplateDirectoryValue;

        public static TemplateOptions TemplateOptions => TemplateOptionsValue;

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

        public static IFluidTemplate ParseTemplate(string templateKey, string content)
        {
            if (IsCodeMappingTemplate(templateKey))
            {
                return ParseCodeMapping(content);
            }
            else
            {
                return ParseLiquidTemplate(templateKey, content);
            }
        }

        public static IFluidTemplate ParseCodeMapping(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            try
            {
                var mapping = JsonSerializer.Deserialize<CodeMapping>(content);
                if (mapping?.Mapping == null)
                {
                    throw new TemplateLoadException(FhirConverterErrorCode.InvalidCodeMapping, Resources.InvalidCodeMapping);
                }
            }
            catch (JsonException ex)
            {
                throw new TemplateLoadException(FhirConverterErrorCode.InvalidCodeMapping, Resources.InvalidCodeMapping, ex);
            }

            // Provide an empty template since the data is in the model
            if (!Parser.TryParse(string.Empty, out var template, out var error))
            {
                throw new TemplateLoadException(
                    FhirConverterErrorCode.TemplateSyntaxError,
                    $"Failed to parse code mapping template: {error}");
            }

            return template;
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

        public static bool IsCodeMappingTemplate(string templateKey)
        {
            return templateKey.EndsWith("ValueSet/ValueSet", StringComparison.InvariantCultureIgnoreCase);
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
    }
}