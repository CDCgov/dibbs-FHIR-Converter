// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DotLiquid;
using DotLiquid.Util;
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

        public static string BatchRender(Context context, List<object> collection, string templateName, string variableName, string collectionVarName = null)
        {
            var template = GetTemplate(context, templateName);

            var sb = new StringBuilder();
            context.Stack(() =>
            {
                collection?.ForEach(entry =>
                {
                    context[variableName] = entry;
                    if (!string.IsNullOrWhiteSpace(collectionVarName))
                    {
                        context[collectionVarName] = collection;
                    }

                    var result = template.Render(RenderParameters.FromContext(context, CultureInfo.InvariantCulture));

                    sb.Append(result);
                    sb.Append(',');
                });
            });

            return sb.ToString();
        }

        public static string BatchRenderParallel(Context context, List<object> collection, string templateName, string variableName, string collectionVarName = null)
        {
            var template = GetTemplate(context, templateName);

            var sb = new StringBuilder();
            context.Stack(() =>
            {
                if (collection != null && collection.Any())
                {
                    Parallel.ForEach(collection, entry =>
                    {
                        // Create a new context for each parallel task to avoid race conditions
                        var localContext = new Context(context.Environments, new Hash(), context.Registers, ErrorsOutputMode.Rethrow, context.MaxIterations, CultureInfo.InvariantCulture, CancellationToken.None);

                        foreach (var scope in context.Scopes)
                        {
                            foreach (var key in scope.Keys)
                            {
                                localContext[key] = scope[key];
                            }
                        }

                        localContext[variableName] = entry;
                        if (!string.IsNullOrWhiteSpace(collectionVarName))
                        {
                            localContext[collectionVarName] = collection;
                        }

                        var result = template.Render(RenderParameters.FromContext(localContext, CultureInfo.InvariantCulture));

                        lock (sb)
                        {
                            sb.Append(result);
                            sb.Append(',');
                        }
                    });
                }
            });

            return sb.ToString();
        }

        private static Template GetTemplate(Context context, string templateName)
        {
            var templateFileSystem = context.Registers["file_system"] as IFhirConverterTemplateFileSystem;
            var template = templateFileSystem?.GetTemplate(templateName, context[TemplateUtility.RootTemplateParentPathScope]?.ToString());

            if (template == null)
            {
                throw new RenderException(FhirConverterErrorCode.TemplateNotFound, string.Format(Resources.TemplateNotFound, templateName));
            }

            return template;
        }

        private static bool HasMatchingPropertyRecursive(IEnumerable<object> entry, string keyPath, string targetProperty = null)
        {
            var keys = keyPath.Split(".");
            var thisKey = keys[0];
            var thisTargetProperty = keys.Count() == 1 ? targetProperty : null;
            var res = DotLiquid.StandardFilters.Where(entry, thisKey, thisTargetProperty) as IEnumerable<object>;
            if (res.Count() == 0)
            {
                return false;
            }
            else if (keys.Count() == 1)
            {
                return true;
            }
            else
            {
                return HasMatchingPropertyRecursive(
                    DotLiquid.StandardFilters.Map(res, thisKey) as IEnumerable<object>,
                    string.Join('.', keys[1..]),
                    targetProperty);
            }
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
    }
}
