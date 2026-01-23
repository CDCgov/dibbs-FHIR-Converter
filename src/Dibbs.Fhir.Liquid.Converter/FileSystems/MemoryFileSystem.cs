using System;
using System.Collections.Generic;
using Dibbs.Fhir.Liquid.Converter.Exceptions;
using Dibbs.Fhir.Liquid.Converter.Models;
using Dibbs.Fhir.Liquid.Converter.Utilities;
using Fluid;

namespace Dibbs.Fhir.Liquid.Converter.FileSystems
{
    public class MemoryFileSystem : IFhirConverterTemplateFileSystem
    {
        private readonly List<Dictionary<string, IFluidTemplate>> _templateCollection;

        public MemoryFileSystem(List<Dictionary<string, IFluidTemplate>> templateCollection)
        {
            _templateCollection = new List<Dictionary<string, IFluidTemplate>>();
            foreach (var templates in templateCollection)
            {
                _templateCollection.Add(new Dictionary<string, IFluidTemplate>(templates, StringComparer.OrdinalIgnoreCase));
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
                throw new RenderException(
                    FhirConverterErrorCode.TemplateNotFound,
                    string.Format(Resources.TemplateNotFound, templateName));
            }

            return GetTemplate(templatePath)
                ?? throw new RenderException(
                    FhirConverterErrorCode.TemplateNotFound,
                    string.Format(Resources.TemplateNotFound, templatePath));
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
                if (templates != null && templates.TryGetValue(templateName, out var template))
                {
                    return template;
                }
            }

            return null;
        }

        private string GetTemplatePath(TemplateContext context, string templateName)
        {
            // Get root template's parent path from context if available
            var rootTemplateParentPath = context.GetValue(TemplateUtility.RootTemplateParentPathScope).ToStringValue();

            var templatePath = context.GetValue(templateName).ToStringValue();

            return TemplateUtility.GetFormattedTemplatePath(templatePath, rootTemplateParentPath);
        }
    }
}