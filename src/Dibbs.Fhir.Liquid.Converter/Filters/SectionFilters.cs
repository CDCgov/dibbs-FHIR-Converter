// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Fluid;
using Fluid.Values;

namespace Dibbs.Fhir.Liquid.Converter
{
    /// <summary>
    /// Filters for conversion
    /// </summary>
    public partial class Filters
    {
        [GeneratedRegex("[^A-Za-z0-9]", RegexOptions.IgnoreCase)]
        private static partial Regex NormalizeSectionNameRegex();

        /// <summary>
        /// Returns first instance (non-alphanumeric chars replace by '_' in name) of the sections by template id
        /// Note: the ability to include multiple templateIds was removed to simplify the code because we weren't using it
        /// </summary>
        /// <param name="input">Sections from input message</param>
        /// <param name="arguments">The template ID to search for</param>
        /// <param name="context">The current template context</param>
        /// <returns>The first section with the matching template ID</returns>
        public static async ValueTask<FluidValue> GetFirstCcdaSectionsByTemplateId(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            if (input.IsNil())
            {
                throw new NullReferenceException("Input data cannot be null.");
            }

            if (arguments.At(0).IsNil())
            {
                throw new NullReferenceException("Template id content cannot be null.");
            }

            var result = new Dictionary<string, object>();

            if (input is DictionaryValue inputDict)
            {
                var components = await GetComponents(inputDict, context);

                if (components.IsNil())
                {
                    return FluidValue.Create(result, context.Options);
                }

                var templateIdContent = arguments.At(0).ToStringValue();
                var templateIds = templateIdContent.Split("|", StringSplitOptions.RemoveEmptyEntries);
                foreach (var templateId in templateIds)
                {
                    foreach (var component in components.Enumerate(context))
                    {
                        if (component is DictionaryValue componentDict
                            && (await componentDict.GetValueAsync("section", context)) is DictionaryValue sectionDict)
                        {
                            var templateIdSection = await sectionDict.GetValueAsync("templateId", context);
                            if (!templateIdSection.IsNil())
                            {
                                var sectionJson = (await Fluid.Filters.MiscFilters.Json(templateIdSection, FilterArguments.Empty, context)).ToStringValue();
                                if (sectionJson.Contains(templateId, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    result[NormalizeSectionName(templateId)] = sectionDict;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return FluidValue.Create(result, context.Options);
        }

        private static async Task<FluidValue> GetComponents(DictionaryValue data, TemplateContext context)
        {
            var dataComponents = await GetChildSection(["ClinicalDocument", "component", "structuredBody", "component"], data, context);

            if (dataComponents.IsNil())
            {
                return NilValue.Instance;
            }

            if (dataComponents is ArrayValue dataArray)
            {
                return dataArray;
            }

            return (ArrayValue)dataComponents;
        }

        private static async Task<FluidValue> GetChildSection(string[] sectionNames, DictionaryValue data, TemplateContext context)
        {
            var childSection = await data.GetValueAsync(sectionNames.First(), context);

            if (sectionNames.Length == 1)
            {
                return childSection;
            }
            else
            {
                if (childSection is DictionaryValue sectionDict)
                {
                    return await GetChildSection(sectionNames[1..], sectionDict, context);
                }
                else
                {
                    return NilValue.Instance;
                }
            }
        }

        private static string NormalizeSectionName(string input)
        {
            return NormalizeSectionNameRegex().Replace(input, "_");
        }
    }
}
