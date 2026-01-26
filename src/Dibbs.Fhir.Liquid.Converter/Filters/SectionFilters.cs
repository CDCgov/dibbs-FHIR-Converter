// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dibbs.Fhir.Liquid.Converter.Utilities;
using Fluid;
using Fluid.Values;

namespace Dibbs.Fhir.Liquid.Converter
{
    /// <summary>
    /// Filters for conversion
    /// </summary>
    public partial class Filters
    {
        private static readonly Regex NormalizeSectionNameRegex = new Regex("[^A-Za-z0-9]");

        // Note: I removed the ability to include multiple templateIds to simplify the code because we weren't using it
        public static ValueTask<FluidValue> GetFirstCcdaSectionsByTemplateId(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            var result = new Dictionary<string, object>();

            if (input is DictionaryValue inputDict)
            {
                var components = GetComponents(inputDict, context);

                if (components.IsNil())
                {
                    return NilValue.Instance;
                }

                var templateId = arguments.At(0).ToStringValue();

                foreach (var component in components.Enumerate(context))
                {
                    if (component is DictionaryValue componentDict
                        && componentDict.GetValueAsync("section", context).Result is DictionaryValue sectionDict)
                    {
                        var templateIdSection = sectionDict.GetValueAsync("templateId", context).Result;
                        if (!templateIdSection.IsNil())
                        {
                            var sectionJson = Fluid.Filters.MiscFilters.Json(templateIdSection, FilterArguments.Empty, context).Result.ToStringValue();
                            if (sectionJson.Contains(templateId, StringComparison.InvariantCultureIgnoreCase))
                            {
                                result[NormalizeSectionName(templateId)] = sectionDict;
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                return NilValue.Instance;
            }

            return FluidValue.Create(result, TemplateUtility.TemplateOptions);
        }

        private static FluidValue GetComponents(DictionaryValue data, TemplateContext context)
        {
            var dataComponents = (((data.GetValueAsync("ClinicalDocument", context).Result as DictionaryValue)?
                .GetValueAsync("component", context).Result as DictionaryValue)?
                .GetValueAsync("structuredBody", context).Result as DictionaryValue)?
                .GetValueAsync("component", context).Result;

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

        private static string NormalizeSectionName(string input)
        {
            return NormalizeSectionNameRegex.Replace(input, "_");
        }
    }
}
