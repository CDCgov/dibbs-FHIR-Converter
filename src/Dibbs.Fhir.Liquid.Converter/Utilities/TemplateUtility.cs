// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Dibbs.Fhir.Liquid.Converter.Exceptions;
using Dibbs.Fhir.Liquid.Converter.Models;
using Fluid;
using Newtonsoft.Json;

namespace Dibbs.Fhir.Liquid.Converter.Utilities
{
    public static class TemplateUtility
    {
        private static string templateDirectory = Environment.GetEnvironmentVariable("TEMPLATES_PATH") ?? "../../data/Templates/eCR";

        // We only use one instance of TemplateOptions because it caches templates that are referenced by include or render tags and we can re-use them for other eCRs
        private static TemplateOptions templateOptions;

        // Instantiating a FluidParser instance is expensive
        // so this is the one we will use across the whole application
        public static readonly FluidParser Parser;

        static TemplateUtility()
        {
            Parser = new FluidParser();
            Parser.RegisterParserTag("evaluate", EvaluateParser.Parser, async (evaluateTag, w, e, c) =>
            {
                return await evaluateTag.WriteToAsync(w, e, c);
            });

            // CollectionFilters
            templateOptions = new TemplateOptions();
            templateOptions.Filters.AddFilter("to_array", Filters.ToArray);
            templateOptions.Filters.AddFilter("batch_render", Filters.BatchRender);
            templateOptions.Filters.AddFilter("nested_where", Filters.NestedWhere);

            // CustomFilters
            templateOptions.Filters.AddFilter("clean_string_from_tabs", Filters.CleanStringFromTabs);
            templateOptions.Filters.AddFilter("print_object", Filters.PrintObject);
            templateOptions.Filters.AddFilter("get_loinc_name", Filters.GetLoincName);
            templateOptions.Filters.AddFilter("get_snomed_name", Filters.GetSnomedName);
            templateOptions.Filters.AddFilter("get_rxnorm_name", Filters.GetRxnormName);
            templateOptions.Filters.AddFilter("find_inner_text_by_id", Filters.FindInnerTextById);
            templateOptions.Filters.AddFilter("format_quantity", Filters.FormatQuantity);

            // DateFilters
            templateOptions.Filters.AddFilter("add_hyphens_date", Filters.AddHyphensDate);
            templateOptions.Filters.AddFilter("format_as_date_time", Filters.FormatAsDateTime);
            templateOptions.Filters.AddFilter("now", Filters.Now);
            templateOptions.Filters.AddFilter("format_width_as_period", Filters.FormatWidthAsPeriod);

            // GeneralFilters
            templateOptions.Filters.AddFilter("prepend_id", Filters.PrependID);
            templateOptions.Filters.AddFilter("generate_uuid", Filters.GenerateUUID);
            templateOptions.Filters.AddFilter("get_property", Filters.GetProperty);
            templateOptions.Filters.AddFilter("remove_prefix", Filters.RemovePrefix);

            // SectionFilters
            templateOptions.Filters.AddFilter("get_first_ccda_sections_by_template_id", Filters.GetFirstCcdaSectionsByTemplateId);

            // StringFilters
            templateOptions.Filters.AddFilter("remove_regex", Filters.RemoveRegex);
            templateOptions.Filters.AddFilter("match", Filters.Match);
            templateOptions.Filters.AddFilter("to_xhtml", Filters.ToXhtml);
            templateOptions.Filters.AddFilter("escape_special_chars", Filters.EscapeSpecialChars);
            templateOptions.Filters.AddFilter("prepend", Filters.Prepend);
            templateOptions.Filters.AddFilter("append", Filters.Append);
            templateOptions.Filters.AddFilter("to_json_string", Filters.ToJsonString);
            templateOptions.Filters.AddFilter("gzip", Filters.Gzip);
        }

        public static string RootTemplateParentPathScope => "RootTemplateParentPath";

        public static string RootTemplate => "EICR";

        public static string TemplateDirectory => templateDirectory;

        public static TemplateOptions TemplateOptions => templateOptions;

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
                var mapping = JsonConvert.DeserializeObject<CodeMapping>(content);
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

        public static string GetFormattedTemplatePath(string templateName, string rootTemplateParentPath = "")
        {
            return string.IsNullOrEmpty(rootTemplateParentPath) ? templateName : string.Format("{0}/{1}", rootTemplateParentPath, templateName);
        }
    }
}