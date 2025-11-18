// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using EnsureThat;
using Fluid;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Liquid.Converter.Extensions;
using Microsoft.Health.Fhir.Liquid.Converter.Models;
using Microsoft.Health.Fhir.Liquid.Converter.Models.Json;
using Microsoft.Health.Fhir.Liquid.Converter.Parsers;
using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace Microsoft.Health.Fhir.Liquid.Converter.Processors
{
    public class JsonProcessor : BaseProcessor
    {
        private readonly IDataParser _parser;

        public JsonProcessor(ProcessorSettings processorSettings, ILogger<JsonProcessor> logger)
            : this(processorSettings, new JsonDataParser(), logger)
        {
        }

        public JsonProcessor(ProcessorSettings processorSettings, IDataParser parser, ILogger<JsonProcessor> logger)
            : base(processorSettings, logger)
        {
            _parser = EnsureArg.IsNotNull(parser, nameof(parser));
        }

        protected override DefaultRootTemplateParentPath DefaultRootTemplateParentPath { get; set; } = DefaultRootTemplateParentPath.Json;

        protected override string InternalConvert(string data, string rootTemplate, ITemplateProvider templateProvider, TraceInfo traceInfo = null)
        {
            object jsonData = _parser.Parse(data);
            return InternalConvertFromObject(jsonData, rootTemplate, templateProvider, traceInfo);
        }

        public string Convert(JObject data, string rootTemplate, ITemplateProvider templateProvider, TraceInfo traceInfo = null)
        {
            var jsonData = data.ToObject();
            return InternalConvertFromObject(jsonData, rootTemplate, templateProvider, traceInfo);
        }

        protected override TemplateContext CreateContext(ITemplateProvider templateProvider, IDictionary<string, object> data, string rootTemplate)
        {
            // var cancellationToken = Settings.TimeOut > 0
            //     ? new CancellationTokenSource(Settings.TimeOut).Token
            //     : CancellationToken.None;

            var options = new TemplateOptions
            {
                MemberAccessStrategy = new DefaultMemberAccessStrategy(),
            };

            // TODO: manually add all filters by name :(
            options.Filters.AddFilter(typeof(Filters));

            var context = new JSchemaContext(data, options);

            context.SetValue("file_system", templateProvider.GetTemplateFileSystem());
            AddRootTemplatePathScope(context, templateProvider, rootTemplate);

            // TODO: If you need cancellation token support, attach it via Items
            // context.Items["cancellationToken"] = cancellationToken;

            return context;
        }

        protected override void CreateTraceInfo(object data, TemplateContext context, TraceInfo traceInfo)
        {
            if ((traceInfo is JSchemaTraceInfo jsonTraceInfo) && (context is JSchemaContext jsonContext))
            {
                jsonTraceInfo.ValidateSchemas = jsonContext.ValidateSchemas;
            }
        }
    }
}
