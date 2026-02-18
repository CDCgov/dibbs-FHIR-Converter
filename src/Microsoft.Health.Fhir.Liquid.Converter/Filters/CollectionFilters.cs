// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fluid;
using Fluid.Values;
using Microsoft.Health.Fhir.Liquid.Converter.DotLiquids;
using Microsoft.Health.Fhir.Liquid.Converter.Exceptions;
using Microsoft.Health.Fhir.Liquid.Converter.Models;
using Microsoft.Health.Fhir.Liquid.Converter.Utilities;

namespace Microsoft.Health.Fhir.Liquid.Converter
{
    /// <summary>
    /// Filters for conversion
    /// </summary>
    public partial class Filters
    {
        /// <summary>
        /// Returns an array created (if needed) from given object
        /// </summary>
        /// <param name="input">Object to convert to array</param>
        /// <param name="arguments">Filter arguments (unused)</param>
        /// <param name="context">The current template context (unused)</param>
        /// <returns>An array containing the input object</returns>
        public static ValueTask<FluidValue> ToArray(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            return input switch
            {
                NilValue => ArrayValue.Empty,
                ArrayValue enumerableObject => enumerableObject,
                _ => new ArrayValue([input])
            };
        }

        /// <summary>
        /// Render every entry in a collection with a snippet and a variable name set in snippet
        /// </summary>
        /// <param name="input">A collection of items.</param>
        /// <param name="arguments">At 0: The name of the template to render
        ///     At 1: variable name that will be used for the individual input elements during each iteration
        ///     At 2: (Optional) if included, sets the whole input collection as variable in context using this name</param>
        /// <param name="context">The current template context</param>
        /// <returns>The rendered templates as a string</returns>
        public static async ValueTask<FluidValue> BatchRender(
            FluidValue input,
            FilterArguments arguments,
            TemplateContext context)
        {
            var templateName = arguments.At(0).ToStringValue();
            var variableName = arguments.At(1).ToStringValue();
            var collectionVarName = arguments.At(2).ToStringValue();
            var template = GetTemplate(context, templateName);
            var sb = new StringBuilder();

            if (input is not ArrayValue inputArray)
            {
                return StringValue.Empty;
            }

            foreach (var entry in inputArray.Enumerate(context))
            {
                context.SetValue(variableName, entry);
                if (!string.IsNullOrWhiteSpace(collectionVarName))
                {
                    context.SetValue(collectionVarName, inputArray);
                }

                var result = await template.RenderAsync(context);
                sb.Append(result);
                sb.Append(',');
            }

            return StringValue.Create(sb.ToString());
        }

        // TODO: I'd like to explore this further when we get the chance
        /* public static async ValueTask<FluidValue> BatchRenderParallel(
                FluidValue input,
                FilterArguments arguments,
                TemplateContext context)
        {
            if (input.IsNil()) {
                return StringValue.Empty;
            }

            // Convert input to collection
            var collection = input.ToObjectValue() as IEnumerable<object>;
            if (collection == null) {
                throw new ArgumentException("BatchRenderParallel filter requires a list as input.");
            }

            var templateName = arguments.At(0).ToStringValue();
            var variableName = arguments.At(1).ToStringValue();
            var collectionVarName = arguments.Count > 2 ? arguments.At(2).ToStringValue() : null;
            var template = GetTemplate(context, templateName);

            var results = new ConcurrentQueue<string>();

            // Run parallel rendering
            var tasks = collection.Select(async entry =>
            {
                context.SetValue(variableName, entry);
                if (!string.IsNullOrWhiteSpace(collectionVarName))
                {
                    context.SetValue(collectionVarName, collection.ToList());
                }

                var rendered = await template.RenderAsync(context);
                results.Enqueue(rendered);
            });

            await Task.WhenAll(tasks);

            // Concatenate results with commas
            var sb = new StringBuilder();
            while (results.TryDequeue(out var item))
            {
                sb.Append(item);
                sb.Append(',');
            }

            if (sb.Length > 0) {
                sb.Length--; // Remove trailing comma
            }

            return StringValue.Create(sb.ToString());
        } */

        private static bool HasMatchingPropertyRecursive(IEnumerable<FluidValue> entries, string keyPath, TemplateContext context, FluidValue targetProperty)
        {
            if (entries == null || string.IsNullOrEmpty(keyPath))
            {
                return false;
            }

            var keys = keyPath.Split('.', StringSplitOptions.RemoveEmptyEntries);
            var thisKey = keys[0];
            var thisTargetProperty = keys.Length == 1 ? targetProperty : NilValue.Instance;

            // Filter entries where this key matches the target property (if provided)
            var filtered = entries
                .Select(async e => e is DictionaryValue entryDict ? await entryDict.GetValueAsync(thisKey, context) : NilValue.Instance)
                .Where(v =>
                    {
                        var value = v.Result;
                        return !value.IsNil() && (thisTargetProperty.IsNil() || value.ToStringValue() == thisTargetProperty.ToStringValue());
                    })
                .ToList();

            if (filtered.Count == 0)
            {
                return false;
            }

            if (keys.Length == 1)
            {
                return true;
            }

            // Recurse into nested entries
            var nextPath = string.Join('.', keys[1..]);
            var nextEntries = new List<FluidValue>();

            foreach (var item in filtered)
            {
                if (item.Result is ArrayValue list)
                {
                    nextEntries.AddRange(list.Enumerate(context));
                }
                else
                {
                    nextEntries.Add(item.Result);
                }
            }

            return HasMatchingPropertyRecursive(nextEntries, nextPath, context, targetProperty);
        }

        /// <summary>
        /// Given a collection, return items that match the keypath and target property (like standard
        /// `where` filter except instead of taking one key, takes a period-delimited path of keys).
        /// </summary>
        /// <param name="input">A collection of items.</param>
        /// <param name="arguments">At 0: A period delimited set of keys to search.
        ///     (Optional) the expected value of the item at the end of the keypath, if not present, truthiness is tested.</param>
        /// <param name="context">The current template context</param>
        /// <returns>A list of the items that match. If no matching elements are found, an empty list is returned.</returns>
        public static ValueTask<FluidValue> NestedWhere(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            if (input.IsNil())
            {
                return NilValue.Instance;
            }

            var castedInput = input as ArrayValue;
            var inputEnumerable = castedInput.Enumerate(context);
            var filteredInput = inputEnumerable.Where(entry => HasMatchingPropertyRecursive(new List<FluidValue>() { entry }, arguments.At(0).ToStringValue(), context, arguments.At(1)));

            return new ArrayValue(filteredInput.ToList());
        }

        private static IFluidTemplate GetTemplate(TemplateContext context, string templateName)
        {
            // Using this rather than context.Options.FileProvider since TemplateFileSystem handles caching for us
            var templateFileSystem = context.GetValue("file_system").ToObjectValue() as IFhirConverterTemplateFileSystem;
            var template = templateFileSystem?.GetTemplate(templateName, context.GetValue(TemplateUtility.RootTemplateParentPathScope).ToStringValue());

            if (template == null)
            {
                throw new RenderException(FhirConverterErrorCode.TemplateNotFound, string.Format(Resources.TemplateNotFound, templateName));
            }

            return template;
        }
    }
}