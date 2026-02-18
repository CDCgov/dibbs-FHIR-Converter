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
using Microsoft.Health.Fhir.Liquid.Converter.Parsers;
using Microsoft.Health.Fhir.Liquid.Converter.Utilities;

namespace Microsoft.Health.Fhir.Liquid.Converter.Processors
{
    public class CcdaProcessor : BaseProcessor
    {
        private readonly IDataParser _parser = new CcdaDataParser();
        private CodeMapping codeMapping;

        public CcdaProcessor(ProcessorSettings processorSettings, ILogger<CcdaProcessor> logger, TemplateOptions options)
            : base(processorSettings, logger)
        {
            TemplateOptions = options;
        }

        protected override DefaultRootTemplateParentPath DefaultRootTemplateParentPath { get; set; } = DefaultRootTemplateParentPath.Ccda;

        protected override string InternalConvert(string data, string rootTemplate, ITemplateProvider templateProvider, TraceInfo traceInfo = null)
        {
            object ccdaData = _parser.Parse(data);
            return InternalConvertFromObject(ccdaData, rootTemplate, templateProvider, traceInfo);
        }

        protected override TemplateContext CreateContext(ITemplateProvider templateProvider, IDictionary<string, object> data, string rootTemplate)
        {
            // Load value set mapping
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

        private string GetCodeMappingTemplatePath(TemplateContext context)
        {
            var rootTemplateParentPathFluid = context.GetValue(TemplateUtility.RootTemplateParentPathScope);
            var rootTemplateParentPath = rootTemplateParentPathFluid.IsNil() ? null : rootTemplateParentPathFluid.ToStringValue();
            var codeSystemTemplateName = "ValueSet/ValueSet";
            return TemplateUtility.GetFormattedTemplatePath(codeSystemTemplateName, rootTemplateParentPath);
        }
    }
}
