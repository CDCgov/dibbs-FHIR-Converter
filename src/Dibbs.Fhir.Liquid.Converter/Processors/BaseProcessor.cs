// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using Dibbs.Fhir.Liquid.Converter.Exceptions;
using Dibbs.Fhir.Liquid.Converter.Models;
using Dibbs.Fhir.Liquid.Converter.OutputProcessors;
using Dibbs.Fhir.Liquid.Converter.Utilities;
using EnsureThat;
using Fluid;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dibbs.Fhir.Liquid.Converter.Processors
{
    public abstract class BaseProcessor : IFhirConverter
    {
        protected BaseProcessor(ILogger<BaseProcessor> logger)
        {
            Logger = EnsureArg.IsNotNull(logger, nameof(logger));
        }

        protected ILogger<BaseProcessor> Logger { get; }

        protected virtual string DataKey { get; set; } = "msg";

        protected virtual DefaultRootTemplateParentPath DefaultRootTemplateParentPath { get; set; }

        public string Convert(string data, string rootTemplate, string templatesPath, ITemplateProvider templateProvider)
        {
            string result = InternalConvert(data, rootTemplate, templatesPath, templateProvider);
            return result;
        }

        protected abstract string InternalConvert(string data, string rootTemplate, string templatesPath, ITemplateProvider templateProvider);

        protected virtual TemplateContext CreateContext(ITemplateProvider templateProvider, IDictionary<string, object> data, string rootTemplate)
        {
            // We initialize Fluid's file provider which is used for includes
            var fileProvider = new PhysicalFileProvider(Path.GetFullPath(TemplateUtility.TemplateDirectory));
            TemplateUtility.TemplateOptions.FileProvider = fileProvider;
            var context = new TemplateContext(data, TemplateUtility.TemplateOptions);

            // Used later for batch rendering since TemplateFileSystem handles caching for us
            context.SetValue("file_system", templateProvider.GetTemplateFileSystem());

            return context;
        }

        protected string InternalConvertFromObject(object data, string rootTemplate, string templatesPath, ITemplateProvider templateProvider)
        {
            if (string.IsNullOrEmpty(rootTemplate))
            {
                throw new RenderException(FhirConverterErrorCode.NullOrEmptyRootTemplate, Resources.NullOrEmptyRootTemplate);
            }

            if (templateProvider == null)
            {
                throw new RenderException(FhirConverterErrorCode.NullTemplateProvider, Resources.NullTemplateProvider);
            }

            rootTemplate = templateProvider.IsDefaultTemplateProvider ? string.Format("{0}/{1}", DefaultRootTemplateParentPath, rootTemplate) : rootTemplate;

            IFluidTemplate template = templateProvider.GetTemplate(rootTemplate);
            if (template == null)
            {
                throw new RenderException(FhirConverterErrorCode.TemplateNotFound, string.Format(Resources.TemplateNotFound, rootTemplate));
            }

            var dictionary = new Dictionary<string, object> { { DataKey, data } };
            var context = CreateContext(templateProvider, dictionary, rootTemplate);
            string rawResult = RenderTemplates(template, context);
            JObject result = PostProcessor.Process(rawResult);

            return result.ToString(Formatting.Indented);
        }

        protected string RenderTemplates(IFluidTemplate template, TemplateContext context)
        {
            try
            {
                return template.Render(context);
            }
            catch (RenderException)
            {
                throw;
            }
            catch (TemplateLoadException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ex: {1} StackTrace: '{0}'", Environment.StackTrace, ex);
                throw new RenderException(FhirConverterErrorCode.TemplateRenderingError, string.Format(Resources.TemplateRenderingError, ex.Message), ex);
            }
        }
    }
}