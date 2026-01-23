// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Dibbs.Fhir.Liquid.Converter.DataParsers;
using Dibbs.Fhir.Liquid.Converter.Models;
using Dibbs.Fhir.Liquid.Converter.Utilities;
using Fluid;
using Fluid.Values;
using Microsoft.Extensions.Logging;

namespace Dibbs.Fhir.Liquid.Converter.Processors
{
    public class CcdaProcessor : BaseProcessor
    {
        private readonly CcdaDataParser parser = new CcdaDataParser();
        private readonly CodeMapping codeMapping;

        public CcdaProcessor(ILogger<CcdaProcessor> logger)
            : base(logger)
        {
            var codeMappingText = File.ReadAllText(GetCodeMappingTemplatePath());
            codeMapping = JsonSerializer.Deserialize<CodeMapping>(codeMappingText);
        }

        protected override DefaultRootTemplateParentPath DefaultRootTemplateParentPath { get; set; } = DefaultRootTemplateParentPath.Ccda;

        protected override string InternalConvert(string data, string rootTemplate, string templatesPath, ITemplateProvider templateProvider)
        {
            object ccdaData = parser.Parse(data);
            return InternalConvertFromObject(ccdaData, rootTemplate, templatesPath, templateProvider);
        }

        protected override TemplateContext CreateContext(ITemplateProvider templateProvider, IDictionary<string, object> data, string rootTemplate)
        {
            var context = base.CreateContext(templateProvider, data, rootTemplate);

            if (codeMapping.Mapping.Keys.Count > 0)
            {
                context.SetValue("CodeMapping", new ObjectValue(codeMapping));
            }

            return context;
        }

        private static string GetCodeMappingTemplatePath()
        {
            var codeSystemTemplateName = "ValueSet/ValueSet.json";
            return TemplateUtility.GetFormattedTemplatePath(codeSystemTemplateName, TemplateUtility.TemplateDirectory);
        }
    }
}