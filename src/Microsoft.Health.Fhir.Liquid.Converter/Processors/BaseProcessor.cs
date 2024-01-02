﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using DotLiquid;
using EnsureThat;
using Microsoft.Health.Fhir.Liquid.Converter.Exceptions;
using Microsoft.Health.Fhir.Liquid.Converter.Models;
using Microsoft.Health.Fhir.Liquid.Converter.OutputProcessors;
using Microsoft.Health.Fhir.Liquid.Converter.Utilities;
using Newtonsoft.Json;

namespace Microsoft.Health.Fhir.Liquid.Converter.Processors
{
    public abstract class BaseProcessor : IFhirConverter
    {
        protected BaseProcessor(ProcessorSettings processorSettings)
        {
            Settings = EnsureArg.IsNotNull(processorSettings, nameof(processorSettings));
        }

        public ProcessorSettings Settings { get; }

        protected virtual string DataKey { get; set; } = "msg";

        protected virtual DataType DataType { get; set; }

        public string Convert(string data, string rootTemplate, ITemplateProvider templateProvider, CancellationToken cancellationToken, TraceInfo traceInfo = null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Convert(data, rootTemplate, templateProvider, traceInfo);
        }

        public abstract string Convert(string data, string rootTemplate, ITemplateProvider templateProvider, TraceInfo traceInfo = null);

        protected virtual Context CreateContext(ITemplateProvider templateProvider, IDictionary<string, object> data, string rootTemplate)
        {
            // Load data and templates
            var cancellationToken = Settings.TimeOut > 0 ? new CancellationTokenSource(Settings.TimeOut).Token : CancellationToken.None;
            var context = new Context(
                environments: new List<Hash> { Hash.FromDictionary(data) },
                outerScope: new Hash(),
                registers: Hash.FromDictionary(new Dictionary<string, object> { { "file_system", templateProvider.GetTemplateFileSystem() } }),
                errorsOutputMode: ErrorsOutputMode.Rethrow,
                maxIterations: Settings.MaxIterations,
                formatProvider: CultureInfo.InvariantCulture,
                cancellationToken: cancellationToken);

            // Load filters
            context.AddFilters(typeof(Filters));

            // Add root template's parent path to context.
            AddRootTemplatePathScope(context, templateProvider, rootTemplate);

            return context;
        }

        protected virtual void CreateTraceInfo(object data, Context context, TraceInfo traceInfo)
        {
        }

        protected string Convert(object data, string rootTemplate, ITemplateProvider templateProvider, TraceInfo traceInfo = null)
        {
            if (string.IsNullOrEmpty(rootTemplate))
            {
                throw new RenderException(FhirConverterErrorCode.NullOrEmptyRootTemplate, Resources.NullOrEmptyRootTemplate);
            }

            if (templateProvider == null)
            {
                throw new RenderException(FhirConverterErrorCode.NullTemplateProvider, Resources.NullTemplateProvider);
            }

            rootTemplate = templateProvider.IsDefaultTemplateProvider ? string.Format("{0}/{1}", DataType, rootTemplate) : rootTemplate;
            var template = templateProvider.GetTemplate(rootTemplate);
            if (template == null)
            {
                throw new RenderException(FhirConverterErrorCode.TemplateNotFound, string.Format(Resources.TemplateNotFound, rootTemplate));
            }

            var dictionary = new Dictionary<string, object> { { DataKey, data } };
            var context = CreateContext(templateProvider, dictionary, rootTemplate);
            var rawResult = RenderTemplates(template, context);
            var result = PostProcessor.Process(rawResult);
            CreateTraceInfo(data, context, traceInfo);

            return result.ToString(Formatting.Indented);
        }

        protected string RenderTemplates(Template template, Context context)
        {
            try
            {
                template.MakeThreadSafe();
                return template.Render(RenderParameters.FromContext(context, CultureInfo.InvariantCulture));
            }
            catch (TimeoutException ex)
            {
                throw new RenderException(FhirConverterErrorCode.TimeoutError, Resources.TimeoutError, ex);
            }
            catch (OperationCanceledException ex)
            {
                throw new RenderException(FhirConverterErrorCode.TimeoutError, Resources.TimeoutError, ex);
            }
            catch (RenderException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RenderException(FhirConverterErrorCode.TemplateRenderingError, string.Format(Resources.TemplateRenderingError, ex.Message), ex);
            }
        }

        protected void AddRootTemplatePathScope(Context context, ITemplateProvider templateProvider, string rootTemplate)
        {
            // Add path to root template's parent. In case of default template provider, use the data type as the parent path, else use the parent path set in the context.
            context[TemplateUtility.RootTemplateParentPathScope] = templateProvider.IsDefaultTemplateProvider ? DataType : TemplateUtility.GetRootTemplateParentPath(rootTemplate);
        }
    }
}
