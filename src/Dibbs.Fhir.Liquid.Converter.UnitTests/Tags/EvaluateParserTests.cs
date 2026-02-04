using Dibbs.Fhir.Liquid.Converter;
using Fluid.Ast;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests.Tags
{
    public class EvaluateParserTests
    {
        // TODO
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
    }
}