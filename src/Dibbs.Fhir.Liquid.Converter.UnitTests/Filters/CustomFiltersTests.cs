using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fluid;
using Fluid.Values;
using Hl7.FhirPath.Expressions;
using Xunit;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests;

public class CustomFilterTests
{
    public class GetTerminology()
    {
        public class GetLoincName()
        {
            [Fact]
            public void GetLoincName_ValidLOINC_ReturnsName()
            {
                var loinc = "34565-2";
                var context = new TemplateContext();
                var actual = Filters.GetLoincName(StringValue.Create(loinc), FilterArguments.Empty, context).Result.ToStringValue();
                Assert.Equal("Vital signs, weight and height panel", actual);
            }
            [Fact]
            public void GetLoincName_InvalidLOINC_ReturnsNull()
            {
                var loinc = "ABC";
                var context = new TemplateContext();
                var actual = Filters.GetLoincName(StringValue.Create(loinc), FilterArguments.Empty, context).Result;
                Assert.Equal(NilValue.Instance, actual);
            }

            [Fact]
            public void GetLoincName_Null_ReturnsNull()
            {
                var context = new TemplateContext();
                var actual = Filters.GetLoincName(NilValue.Instance, FilterArguments.Empty, context).Result;
                Assert.Equal(NilValue.Instance, actual);
            }

            [Fact]
            public void GetLoincName_TrailingWhitespace()
            {
                var loinc = "94308-4 ";
                var context = new TemplateContext();
                var actual = Filters.GetLoincName(StringValue.Create(loinc), FilterArguments.Empty, context).Result.ToStringValue();
                Assert.Equal("SARS-CoV-2 (COVID-19) N gene NAA+probe CDC primer-probe set N2 Ql (Specimen)", actual);
            }
        }
        public class GetSnomedName()
        {
            [Fact]
            public void GetSnomedName_ValidSnomed_ReturnsName()
            {
                var code = "100000000";
                var context = new TemplateContext();
                var actual = Filters.GetSnomedName(StringValue.Create(code), FilterArguments.Empty, context).Result.ToStringValue();
                Assert.Equal("BITTER-3", actual);
            }
        }

        public class GetRxNormName()
        {
            [Fact]
            public void GetRxnormName_ValidRxnorm_ReturnsName()
            {
                var rxnorm = "1044916";
                var context = new TemplateContext();
                var actual = Filters.GetRxnormName(StringValue.Create(rxnorm), FilterArguments.Empty, context).Result.ToStringValue();
                Assert.Equal("VioNex", actual);
            }

            [Fact]
            public void GetRxnormName_InvalidRxnorm_ReturnsNull()
            {
                var rxnorm = "ABC";
                var context = new TemplateContext();
                var actual = Filters.GetRxnormName(StringValue.Create(rxnorm), FilterArguments.Empty, context).Result;
                Assert.Equal(NilValue.Instance, actual);
            }

            [Fact]
            public void GetRxnormName_Null_ReturnsNull()
            {
                var context = new TemplateContext();
                var actual = Filters.GetRxnormName(NilValue.Instance, FilterArguments.Empty, context).Result;
                Assert.Equal(NilValue.Instance, actual);
            }
        }
    }

    [Fact]
    public void FindInnerTextById_ValidId_ReturnsString()
    {
        TemplateContext context = new TemplateContext();
        var innerXml = "<paragraph style=\"bold\">hello</paragraph>";
        var fragment = $"<content ID=\"hi\">{innerXml}</content><content ID=\"bye\">bye</content>";
        var actual = Filters.FindInnerTextById(StringValue.Create(fragment), new FilterArguments(StringValue.Create("hi")), context).Result.ToStringValue();
        Assert.Equal(innerXml, actual);
    }

    [Fact]
    public void FindInnerTextById_InvalidId_ReturnsNull()
    {
        TemplateContext context = new TemplateContext();
        var innerXml = "<paragraph style=\"bold\">hello</paragraph>";
        var fragment = $"<content ID=\"hi\">{innerXml}</content><content ID=\"bye\">bye</content>";
        var actual = Filters.FindInnerTextById(StringValue.Create(fragment), new FilterArguments(StringValue.Create("nope")), context).Result;
        Assert.Equal(NilValue.Instance, actual);
    }

    [Fact]
    public void FindInnerTextById_Null_ReturnsNull()
    {
        TemplateContext context = new TemplateContext();
        var actual = Filters.FindInnerTextById(NilValue.Instance, new FilterArguments(StringValue.Create("nope")), context).Result;
        Assert.Equal(NilValue.Instance, actual);
    }

    [Fact]
    public async Task FindObjectByIdRecursive_ValidId_ReturnsObject()
    {
        var options = new TemplateOptions();
        var text1 = new List<object>()
        {
                "Correct return value",
        };
        var content1 = new Dictionary<string, object>()
        {
                { "ID", "test-id-1" },
                { "_", text1 },
        };
        var expected = FluidValue.Create(content1, options);

        var dict1 = new Dictionary<string, object>()
        {
                { "content", content1 },
        };
        var text2 = new List<object>()
        {
                "Incorrect return value",
        };
        var content2 = new Dictionary<string, object>()
        {
                { "ID", "test-id-2" },
                { "_", text2 },
        };
        var dict2 = new Dictionary<string, object>()
        {
                { "content", content2 },
        };
        var data = new List<object>()
        {
                dict1, dict2,
        };

        var value = FluidValue.Create(data, options);

        var arguments = new FilterArguments();
        arguments.Add(StringValue.Create("test-id-1"));
        var actual = await Filters.FindObjectById(value, arguments, new TemplateContext());

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task FindObjectById_NullData_ReturnsNull()
    {
        var arguments = new FilterArguments();
        arguments.Add(StringValue.Create("fake-id"));
        var actual = await Filters.FindObjectById(NilValue.Instance, arguments, new TemplateContext());
        Assert.True(actual.IsNil());
    }
}
