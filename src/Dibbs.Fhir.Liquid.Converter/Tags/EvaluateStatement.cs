using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Dibbs.Fhir.Liquid.Converter.Exceptions;
using Dibbs.Fhir.Liquid.Converter.FileSystems;
using Dibbs.Fhir.Liquid.Converter.Models;
using Dibbs.Fhir.Liquid.Converter.Utilities;
using Fluid;
using Fluid.Ast;
using Fluid.Values;

namespace Dibbs.Fhir.Liquid.Converter
{
    public class EvaluateStatement : Statement
    {
        private readonly string _target;
        private readonly string _templateName;
        private readonly Dictionary<string, Expression> _attributes;

        public EvaluateStatement(
            string target,
            string templateName,
            Dictionary<string, Expression> attributes)
        {
            _target = target;
            _templateName = templateName;
            _attributes = attributes;
        }

        public override async ValueTask<Completion> WriteToAsync(
            TextWriter writer,
            TextEncoder encoder,
            TemplateContext context)
        {
            var templateFileSystem = context.GetValue("file_system").ToObjectValue() as IFhirConverterTemplateFileSystem;
            var template = templateFileSystem?.GetTemplate(_templateName, TemplateUtility.TemplateDirectory);

            if (template == null)
            {
                throw new RenderException(FhirConverterErrorCode.TemplateNotFound, string.Format(Resources.TemplateNotFound, _templateName));
            }

            foreach (var attr in _attributes)
            {
                var value = attr.Value.EvaluateAsync(context).Result;
                context.SetValue(attr.Key, value);
            }

            using var sw = new StringWriter();
            await template.RenderAsync(sw, encoder, context);

            var content = sw.ToString().Trim();
            context.SetValue(_target, content.Length == 0 ? NilValue.Instance : content);

            return Completion.Normal;
        }
    }
}