// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;
using Dibbs.Fhir.Liquid.Converter.DataParsers;
using Dibbs.Fhir.Liquid.Converter.Models;
using Dibbs.Fhir.Liquid.Converter.OutputProcessors;
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

        /// <summary>
        /// Converts an EICR using the supplied document, which is normalized in place.
        /// Other root templates retain the string path for original-XML attachments.
        /// </summary>
        public string Convert(XDocument document, string rootTemplate, string templatesPath, ITemplateProvider templateProvider, IFileProvider fileProvider)
        {
            ArgumentNullException.ThrowIfNull(document);
            if (rootTemplate != "EICR")
            {
                return Convert(document.ToString(SaveOptions.DisableFormatting), rootTemplate, templatesPath, templateProvider, fileProvider);
            }

            var references = EntryReferencePostProcessor.Capture(document);
            var root = document.Root;
            var ids = root?.Elements(root.Name.Namespace + "id").ToArray();
            var hasDocumentId = root != null && root.Name == root.GetDefaultNamespace() + "ClinicalDocument" &&
                ids?.Length == 1 && (ids[0].Attribute("extension") != null || ids[0].Attribute("root") != null);
            var fallbackId = hasDocumentId ? null : CdaDocumentId.Generate(document);
            var data = parser.Parse(document);
            data["_documentIdFallback"] = fallbackId;
            var converted = InternalConvertFromObject(data, rootTemplate, templatesPath, templateProvider, fileProvider);
            return EntryReferencePostProcessor.Process(converted, references);
        }

        protected override string InternalConvert(string data, string rootTemplate, string templatesPath, ITemplateProvider templateProvider, IFileProvider fileProvider)
        {
            object ccdaData = parser.Parse(data);
            string converted = InternalConvertFromObject(ccdaData, rootTemplate, templatesPath, templateProvider, fileProvider);
            return EntryReferencePostProcessor.Process(converted, data);
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
