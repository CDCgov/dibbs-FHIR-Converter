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
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace Dibbs.Fhir.Liquid.Converter.Processors
{
    public class CcdaProcessor : BaseProcessor
    {
        private readonly CcdaDataParser parser = new CcdaDataParser();
        private readonly CodeMapping codeMapping;

        public CcdaProcessor(ILogger<CcdaProcessor> logger, TemplateOptions options)
            : base(logger)
        {
            var codeMappingText = File.ReadAllText(GetCodeMappingTemplatePath());
            codeMapping = JsonSerializer.Deserialize<CodeMapping>(codeMappingText);
            TemplateOptions = options;
        }

        protected override string DefaultRootTemplateParentPath { get; set; } = "eCR";

        protected override string InternalConvert(string data, string rootTemplate, string templatesPath, ITemplateProvider templateProvider, IFileProvider fileProvider)
        {
            object ccdaData = parser.Parse(data);
            return InternalConvertFromObject(ccdaData, rootTemplate, templatesPath, templateProvider, fileProvider);
        }

        protected override TemplateContext CreateContext(ITemplateProvider templateProvider, IDictionary<string, object> data, string rootTemplate, IFileProvider fileProvider)
        {
            var context = base.CreateContext(templateProvider, data, rootTemplate, fileProvider);

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