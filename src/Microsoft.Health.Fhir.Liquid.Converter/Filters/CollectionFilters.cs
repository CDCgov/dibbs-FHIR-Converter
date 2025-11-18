// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
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
        public static List<object> ToArray(object input)
        {
            return input switch
            {
                null => new List<object>(),
                IEnumerable<object> enumerableObject => enumerableObject.ToList(),
                _ => new List<object> { input }
            };
        }

        public static List<object> Concat(List<object> l1, List<object> l2)
        {
            return new List<object>().Concat(l1 ?? new List<object>()).Concat(l2 ?? new List<object>()).ToList();
        }

        public static async ValueTask<FluidValue> BatchRenderAsync(
            FluidValue input,
            FilterArguments arguments,
            TemplateContext context)
        {
            if (input.IsNil()) {
                return StringValue.Empty;
            }

            var collection = input.ToObjectValue() as List<object>;
            if (collection == null) {
                throw new ArgumentException("BatchRender filter requires a list as input.");
            }

            var templateName = arguments.At(0).ToStringValue();
            var variableName = arguments.At(1).ToStringValue();
            var collectionVarName = arguments.Count > 2 ? arguments.At(2).ToStringValue() : null;
            var template = GetTemplate(context, templateName);
            var sb = new StringBuilder();

            foreach (var entry in collection)
            {
                context.EnterChildScope();
                try
                {
                    context.SetValue(variableName, entry);
                    if (!string.IsNullOrWhiteSpace(collectionVarName))
                    {
                        context.SetValue(collectionVarName, collection);
                    }

                    var result = await template.RenderAsync(context);
                    sb.Append(result);
                    sb.Append(',');
                }
                finally
                {
                    context.ReleaseScope();
                }
            }

            if (sb.Length > 0) {
                sb.Length--;
            }

            return new StringValue(sb.ToString());
        }

        public static async ValueTask<FluidValue> BatchRenderParallelAsync(
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
                // Create a new context per item to avoid race conditions
                var localContext = CloneContext(context);

                localContext.SetValue(variableName, entry);
                if (!string.IsNullOrWhiteSpace(collectionVarName))
                {
                    localContext.SetValue(collectionVarName, collection.ToList());
                }

                var rendered = await template.RenderAsync(localContext);
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

            return new StringValue(sb.ToString());
        }

        private static bool HasMatchingPropertyRecursive(IEnumerable<object> entries, string keyPath, string targetProperty = null)
        {
            if (entries == null || string.IsNullOrEmpty(keyPath)) {
                return false;
            }

            var keys = keyPath.Split('.', StringSplitOptions.RemoveEmptyEntries);
            var thisKey = keys[0];
            var thisTargetProperty = keys.Length == 1 ? targetProperty : null;

            // Filter entries where this key matches the target property (if provided)
            var filtered = entries
                .Select(e => GetValue(e, thisKey))
                .Where(v => v != null && (thisTargetProperty == null || v.Equals(thisTargetProperty)))
                .ToList();

            if (!filtered.Any()) {
                return false;
            }

            if (keys.Length == 1) {
                return true;
            }

            // Recurse into nested entries
            var nextPath = string.Join('.', keys[1..]);
            var nextEntries = new List<object>();

            foreach (var item in filtered)
            {
                if (item is IEnumerable<object> list)
                {
                    nextEntries.AddRange(list);
                }
                else
                {
                    nextEntries.Add(item);
                }
            }

            return HasMatchingPropertyRecursive(nextEntries, nextPath, targetProperty);
        }

        /// <summary>
        /// Given a collection, return items that match the keypath and target property (like standard
        /// `where` filter except instead of taking one key, takes a period-delimited path of keys).
        /// </summary>
        /// <param name="entries">A collection of items.</param>
        /// <param name="keyPath">A period delimited set of keys to search.</param>
        /// <param name="targetProperty">Optionally, the expected value of the item at the end of the keypath, if not present, truthiness is tested.</param>
        /// <returns>A list of the items that match. If no matching elements are found, an empty list is returned.</returns>
        public static IEnumerable<object> NestedWhere(IEnumerable<object> entries, string keyPath, string targetProperty = null)
        {
            if (entries == null)
            {
                return null;
            }

            return entries.Cast<object>().Where(entry => HasMatchingPropertyRecursive(new[] { entry }, keyPath, targetProperty));
        }

        private static TemplateContext CloneContext(TemplateContext original)
        {
            var options = original.Options;
            var model = new Dictionary<string, object>();

            foreach (var key in original.ValueNames)
            {
                model[key] = original.GetValue(key).ToObjectValue();
            }

            return new TemplateContext(model, options);
        }

        private static IFluidTemplate GetTemplate(TemplateContext context, string templateName)
        {
            var templateFileSystem = context.GetValue("file_system") as IFhirConverterTemplateFileSystem;
            var template = templateFileSystem?.GetTemplate(templateName, context.GetValue(TemplateUtility.RootTemplateParentPathScope)?.ToString());

            if (template == null)
            {
                throw new RenderException(FhirConverterErrorCode.TemplateNotFound, string.Format(Resources.TemplateNotFound, templateName));
            }

            return template;
        }

        /// <summary>
        /// Helper to get a property or dictionary value by key
        /// </summary>
        private static object GetValue(object obj, string key)
        {
            if (obj == null || string.IsNullOrEmpty(key)) {
                return null;
            }

            // If obj is a dictionary
            if (obj is IDictionary<string, object> dict && dict.TryGetValue(key, out var val))
            {
                return val;
            }

            // If obj is a FluidValue (from TemplateContext), unwrap it
            if (obj is FluidValue fv) {
                obj = fv.ToObjectValue();
            }

            // Try reflection for POCOs
            var prop = obj.GetType().GetProperty(key);
            if (prop != null) {
                return prop.GetValue(obj);
            }

            return null;
        }
    }
}
