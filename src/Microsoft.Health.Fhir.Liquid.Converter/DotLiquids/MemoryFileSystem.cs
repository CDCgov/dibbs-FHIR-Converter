// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Fluid;
using Microsoft.Health.Fhir.Liquid.Converter.Exceptions;
using Microsoft.Health.Fhir.Liquid.Converter.Models;
using Microsoft.Health.Fhir.Liquid.Converter.Utilities;

namespace Microsoft.Health.Fhir.Liquid.Converter.DotLiquids
{
    public class MemoryFileSystem : IFhirConverterTemplateFileSystem
    {
        private readonly List<Dictionary<string, IFluidTemplate>> _templateCollection;

        public MemoryFileSystem(List<Dictionary<string, IFluidTemplate>> templateCollection)
        {
            _templateCollection = new List<Dictionary<string, IFluidTemplate>>();
            foreach (var templates in templateCollection)
            {
                _templateCollection.Add(new Dictionary<string, IFluidTemplate>(templates));
            }
        }

        public string ReadTemplateFile(TemplateContext context, string templateName)
        {
            throw new NotImplementedException();
        }

        public IFluidTemplate GetTemplate(TemplateContext context, string templateName)
        {
            var templatePath = GetTemplatePath(context, templateName);
            if (templatePath == null)
            {
                throw new RenderException(FhirConverterErrorCode.TemplateNotFound, string.Format(Resources.TemplateNotFound, templateName));
            }

            return GetTemplate(templatePath) ?? throw new RenderException(FhirConverterErrorCode.TemplateNotFound, string.Format(Resources.TemplateNotFound, templatePath));
        }

        public IFluidTemplate GetTemplate(string templateName, string rootTemplateParentPath = "")
        {
            if (string.IsNullOrEmpty(templateName))
            {
                return null;
            }

            templateName = TemplateUtility.GetFormattedTemplatePath(templateName, rootTemplateParentPath);

            foreach (var templates in _templateCollection)
            {
                if (templates != null && templates.ContainsKey(templateName))
                {
                    return templates[templateName];
                }
            }

            return null;
        }

        private string GetTemplatePath(TemplateContext context, string templateName)
        {
            // Get root template's parent path. This to account for cases where the root template is in a subfolder.
            var rootTemplateParentPath = context[TemplateUtility.RootTemplateParentPathScope]?.ToString();

            var templatePath = context[templateName]?.ToString();

            return TemplateUtility.GetFormattedTemplatePath(templatePath, rootTemplateParentPath);
        }
    }
}
