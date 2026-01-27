using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.Health.Fhir.Liquid.Converter.UnitTests;

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
                var actual = Filters.GetLoincName(loinc);
                Assert.Equal("Vital signs, weight and height panel", actual);
            }
            [Fact]
            public void GetLoincName_InvalidLOINC_ReturnsNull()
            {
                var loinc = "ABC";
                var actual = Filters.GetLoincName(loinc);
                Assert.Null(actual);
            }

            [Fact]
            public void GetLoincName_Null_ReturnsNull()
            {
                var actual = Filters.GetLoincName(null);
                Assert.Null(actual);
            }

            [Fact]
            public void GetLoincName_TrailingWhitespace()
            {
                var loinc = "94308-4 ";
                var actual = Filters.GetLoincName(loinc);
                Assert.Equal("SARS-CoV-2 (COVID-19) N gene NAA+probe CDC primer-probe set N2 Ql (Specimen)", actual);
            }
        }
        public class GetSnomedName()
        {
            [Fact]
            public void GetSnomedName_ValidSnomed_ReturnsName()
            {
                var code = "100000000";
                var actual = Filters.GetSnomedName(code);
                Assert.Equal("BITTER-3", actual);
            }
        }

        public class GetRxNormName()
        {

            [Fact]
            public void GetRxnormName_ValidRxnorm_ReturnsName()
            {
                var rxnorm = "1044916";
                var actual = Filters.GetRxnormName(rxnorm);
                Assert.Equal("VioNex", actual);
            }

            [Fact]
            public void GetRxnormName_InvalidRxnorm_ReturnsNull()
            {
                var rxnorm = "ABC";
                var actual = Filters.GetRxnormName(rxnorm);
                Assert.Null(actual);
            }

            [Fact]
            public void GetRxnormName_Null_ReturnsNull()
            {
                var actual = Filters.GetRxnormName(null);
                Assert.Null(actual);
            }
        }
    }

    [Fact]
    public void FindInnerTextById_ValidId_ReturnsString()
    {
        var innerXml = "<paragraph style=\"bold\">hello</paragraph>";
        var fragment = $"<content ID=\"hi\">{innerXml}</content><content ID=\"bye\">bye</content>";
        var actual = Filters.FindInnerTextById(fragment, "hi");
        Assert.Equal(actual, innerXml);
    }

    [Fact]
    public void FindInnerTextById_InvalidId_ReturnsNull()
    {

        var innerXml = "<paragraph style=\"bold\">hello</paragraph>";
        var fragment = $"<content ID=\"hi\">{innerXml}</content><content ID=\"bye\">bye</content>";
        var actual = Filters.FindInnerTextById(fragment, "nope");
        Assert.Null(actual);
    }

    [Fact]
    public void FindInnerTextById_Null_ReturnsNull()
    {
        var actual = Filters.FindInnerTextById(null, "nope");
        Assert.Null(actual);
    }
}
