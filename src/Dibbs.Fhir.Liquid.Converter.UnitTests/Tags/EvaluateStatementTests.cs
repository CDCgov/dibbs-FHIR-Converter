using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Dibbs.Fhir.Liquid.Converter.Exceptions;
using Dibbs.Fhir.Liquid.Converter.FileSystems;
using Dibbs.Fhir.Liquid.Converter.Models;
using Dibbs.Fhir.Liquid.Converter.Tags;
using Dibbs.Fhir.Liquid.Converter.Utilities;
using Fluid;
using Fluid.Ast;
using Fluid.Values;
using Xunit;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests.Tags
{
    public class EvaluateStatementTests
    {
        private readonly FluidParser parser;
        public EvaluateStatementTests()
        {
            parser = new FluidParser();
        }   

        [Fact]
        public async Task GivenValidInputs_WhenWriteToAsync_ValueShouldBeSetInContext()
        {
            var attributes = new Dictionary<string, Fluid.Ast.Expression>
            {
                {
                    "input",
                    new LiteralExpression(StringValue.Create("foobar"))
                }
            };
            
            var evaluateStatement = new EvaluateStatement("id", "GenerateId", attributes);
            var context = new TemplateContext();
            var content = "id_{{input}}";
            var template = parser.Parse(content);
            var fileSystemDict = new Dictionary<string, IFluidTemplate>
            {
                {
                    "GenerateId",
                    template
                }
            };

            var fileSystem = new MemoryFileSystem(new List<Dictionary<string, IFluidTemplate>>
            {
                fileSystemDict
            });

            context.SetValue("file_system", fileSystem);
            var result = await evaluateStatement.WriteToAsync(new StringWriter(), HtmlEncoder.Default, context);
            Assert.Equal("id_foobar", context.GetValue("id").ToStringValue());
            Assert.Equal(Completion.Normal, result);
        }

        [Fact]
        public async Task GivenTemplateDoesNotExist_WhenWriteToAsync_ShouldThrow()
        {           
            var evaluateStatement = new EvaluateStatement("id", "DNE", new Dictionary<string, Fluid.Ast.Expression>());
            var context = new TemplateContext();
            var fileSystem = new MemoryFileSystem(new List<Dictionary<string, IFluidTemplate>>());
            context.SetValue("file_system", fileSystem);
            
            await Assert.ThrowsAsync<RenderException>(async () => await evaluateStatement.WriteToAsync(new StringWriter(), HtmlEncoder.Default, context));
        }
    }
}