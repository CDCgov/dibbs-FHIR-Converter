using System.Collections.Generic;
using System.Linq;
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
            private TemplateContext context = new TemplateContext();

            [Fact]
            public void GetLoincName_ValidLOINC_ReturnsName()
            {
                var loinc = "34565-2";
                var actual = Filters.GetLoincName(StringValue.Create(loinc), FilterArguments.Empty, context).Result.ToStringValue();
                Assert.Equal("Vital signs, weight and height panel", actual);
            }
            [Fact]
            public void GetLoincName_InvalidLOINC_ReturnsNull()
            {
                var loinc = "ABC";
                var actual = Filters.GetLoincName(StringValue.Create(loinc), FilterArguments.Empty, context).Result;
                Assert.Equal(NilValue.Instance, actual);
            }

            [Fact]
            public void GetLoincName_Null_ReturnsNull()
            {
                var actual = Filters.GetLoincName(NilValue.Instance, FilterArguments.Empty, context).Result;
                Assert.Equal(NilValue.Instance, actual);
            }

            [Fact]
            public void GetLoincName_TrailingWhitespace()
            {
                var loinc = "94308-4 ";
                var actual = Filters.GetLoincName(StringValue.Create(loinc), FilterArguments.Empty, context).Result.ToStringValue();
                Assert.Equal("SARS-CoV-2 (COVID-19) N gene NAA+probe CDC primer-probe set N2 Ql (Specimen)", actual);
            }
        }
        public class GetSnomedName()
        {
            private TemplateContext context = new TemplateContext();

            [Fact]
            public void GetSnomedName_ValidSnomed_ReturnsName()
            {
                var code = "100000000";
                var actual = Filters.GetSnomedName(StringValue.Create(code), FilterArguments.Empty, context).Result.ToStringValue();
                Assert.Equal("BITTER-3", actual);
            }
        }

        public class GetRxNormName()
        {
            private TemplateContext context = new TemplateContext();

            [Fact]
            public void GetRxnormName_ValidRxnorm_ReturnsName()
            {
                var rxnorm = "1044916";
                var actual = Filters.GetRxnormName(StringValue.Create(rxnorm), FilterArguments.Empty, context).Result.ToStringValue();
                Assert.Equal("VioNex", actual);
            }

            [Fact]
            public void GetRxnormName_InvalidRxnorm_ReturnsNull()
            {
                var rxnorm = "ABC";
                var actual = Filters.GetRxnormName(StringValue.Create(rxnorm), FilterArguments.Empty, context).Result;
                Assert.Equal(NilValue.Instance, actual);
            }

            [Fact]
            public void GetRxnormName_Null_ReturnsNull()
            {
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
}
