// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Fluid;
using Fluid.Values;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Liquid.Converter.Models;
using Microsoft.Health.Fhir.Liquid.Converter.Models.Hl7v2;
using Microsoft.Health.Fhir.Liquid.Converter.Parsers;
using Microsoft.Health.Fhir.Liquid.Converter.Utilities;

namespace Microsoft.Health.Fhir.Liquid.Converter.Processors
{
    public class Hl7v2Processor : BaseProcessor
    {
        private readonly IDataParser _parser = new Hl7v2DataParser();
        private CodeMapping codeMapping;

        public Hl7v2Processor(ProcessorSettings processorSettings, ILogger<Hl7v2Processor> logger, TemplateOptions options)
            : base(processorSettings, logger)
        {
            TemplateOptions = options;
        }

        protected override string DataKey { get; set; } = "hl7v2Data";

        protected override DefaultRootTemplateParentPath DefaultRootTemplateParentPath { get; set; } = DefaultRootTemplateParentPath.Hl7v2;

        protected override string InternalConvert(string data, string rootTemplate, ITemplateProvider templateProvider, TraceInfo traceInfo = null)
        {
            object hl7v2Data = _parser.Parse(data);
            return InternalConvertFromObject(hl7v2Data, rootTemplate, templateProvider, traceInfo);
        }

        protected override TemplateContext CreateContext(ITemplateProvider templateProvider, IDictionary<string, object> data, string rootTemplate)
        {
            // Load code system mapping
            var context = base.CreateContext(templateProvider, data, rootTemplate);
            if (codeMapping == null)
            {
                var codeMappingText = File.ReadAllText(GetCodeMappingTemplatePath(context));
                codeMapping = JsonSerializer.Deserialize<CodeMapping>(codeMappingText);
            }

            if (codeMapping.Mapping.Keys.Count > 0)
            {
                context.SetValue("CodeMapping", new ObjectValue(codeMapping));
            }

            return context;
        }

        protected override void CreateTraceInfo(object data, TemplateContext context, TraceInfo traceInfo)
        {
            if (traceInfo is Hl7v2TraceInfo hl7v2TraceInfo)
            {
                hl7v2TraceInfo.UnusedSegments = Hl7v2TraceInfo.CreateTraceInfo(data as Hl7v2Data).UnusedSegments;
            }
        }

        private string GetCodeMappingTemplatePath(TemplateContext context)
        {
            var rootTemplateParentPathFluid = context.GetValue(TemplateUtility.RootTemplateParentPathScope);
            var rootTemplateParentPath = rootTemplateParentPathFluid.IsNil() ? null : rootTemplateParentPathFluid.ToStringValue();
            var codeSystemTemplateName = "CodeSystem/CodeSystem";
            return TemplateUtility.GetFormattedTemplatePath(codeSystemTemplateName, rootTemplateParentPath);
        }
    }
}
