using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Fluid;
using Fluid.Ast;
using Fluid.Values;
using Microsoft.Health.Fhir.Liquid.Converter.DotLiquids;
using Microsoft.Health.Fhir.Liquid.Converter.Exceptions;
using Microsoft.Health.Fhir.Liquid.Converter.Models;
using Microsoft.Health.Fhir.Liquid.Converter.Utilities;

namespace Microsoft.Health.Fhir.Liquid.Converter.Tags
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
            var template = templateFileSystem?.GetTemplate(_templateName, context.GetValue(TemplateUtility.RootTemplateParentPathScope).ToStringValue());

            if (template == null)
            {
                throw new RenderException(FhirConverterErrorCode.TemplateNotFound, string.Format(Resources.TemplateNotFound, _templateName));
            }

            foreach (var attr in _attributes)
            {
                var value = await attr.Value.EvaluateAsync(context);
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