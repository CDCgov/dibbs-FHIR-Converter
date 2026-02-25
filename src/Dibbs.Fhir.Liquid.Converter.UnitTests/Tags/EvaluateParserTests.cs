using Dibbs.Fhir.Liquid.Converter;
using Fluid.Ast;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests.Tags
{
    public class EvaluateParserTests
    {
        [Fact]
        public void GivenValidEvaluateTag_WhenParser_ReturnsEvaluateStatement()
        {
            var input = "id using 'Utils/GenerateId' obj: data";
            var success = EvaluateParser.Parser.TryParse(input, out var actual);
            Assert.True(success);
            var target = actual.GetType().GetField("_target", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.Equal("id", (string)target.GetValue(actual));
            var templateName = actual.GetType().GetField("_templateName", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.Equal("Utils/GenerateId", (string)templateName.GetValue(actual));
            var attributes = actual.GetType().GetField("_attributes", BindingFlags.Instance | BindingFlags.NonPublic);
            var attributesDict = (Dictionary<string, Expression>)attributes.GetValue(actual);
            Assert.Equal(1, attributesDict.Keys.Count);
            var segment = (attributesDict["obj"] as MemberExpression).Segments[0];
            Assert.Equal("data", segment.GetType().GetProperty("Identifier").GetValue(segment));
        }

        [Fact]
        public void GivenInvalidTemplateName_WhenParser_ReturnsCorrectError()
        {
            var input = "id using abc123 obj: data";
            var success = EvaluateParser.Parser.TryParse(input, out var actual, out var error);
            Assert.False(success);
            Assert.Equal("A quoted string value is required for the template name in the evaluate tag", error.Message);
        }

        [Fact]
        public void GivenNoUsing_WhenParser_ReturnsCorrectError()
        {
            var input = "id 'Utils/GenerateId' obj: data";
            var success = EvaluateParser.Parser.TryParse(input, out var actual, out var error);
            Assert.False(success);
            Assert.Equal("Keyword 'using' was expected after the first identifier", error.Message);
        }

        [Fact]
        public void GivenNoArgument_WhenParser_ReturnsCorrectError()
        {
            var input = "id using 'Utils/GenerateId'";
            var success = EvaluateParser.Parser.TryParse(input, out var actual, out var error);
            Assert.False(success);
            Assert.Equal("One argument is expected after template name", error.Message);
        }
    }
}