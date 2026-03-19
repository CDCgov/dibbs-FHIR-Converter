using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests
{
    public class ValueHelperTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory, "Utils", "ValueHelper.liquid"
        );

        [Fact]
        public async System.Threading.Tasks.Task GivenNoAttributeReturnsEmpty()
        {
            await ConvertCheckLiquidTemplate(ECRPath, new Dictionary<string, object>(), "\"valueString\":\"\",");
        }

        [Fact]
        public async System.Threading.Tasks.Task GivenDecimalProperlyReturnsWithDecimal()
        {
            var attributes = new Dictionary<string, object>{
                {"value", new { value = ".29"}}
            };
            await ConvertCheckLiquidTemplate(ECRPath, attributes, "\"valueQuantity\": { \"value\": 0.29, },");
        }

        [Fact]
        public async System.Threading.Tasks.Task GivenIntProperlyReturnsInt()
        {
            var attributes = new Dictionary<string, object>{
                {"value", new { value = "300"}}
            };
            await ConvertCheckLiquidTemplate(ECRPath, attributes, "\"valueQuantity\": { \"value\": 300, },");
        }

        [Fact]
        public async System.Threading.Tasks.Task GivenStringProperlyReturnsString()
        {
            var attributes = new Dictionary<string, object>{
                {"value", new { value = ".50 in"}}
            };
            await ConvertCheckLiquidTemplate(ECRPath, attributes, "\"valueQuantity\": { \"value\": \".50 in\", },");
        }

        [Fact]
        public async System.Threading.Tasks.Task GivenDecimalProperlyReturnsDecimal()
        {
            var attributes = new Dictionary<string, object>{
                {"value", new { value = "300.00"}}
            };
            await ConvertCheckLiquidTemplate(ECRPath, attributes, "\"valueQuantity\": { \"value\": 300.00, },");
        }

        [Fact]
        public async System.Threading.Tasks.Task GivenNegativeValueProperlyReturnsNegativeValue()
        {
            var attributes = new Dictionary<string, object>{
                {"value", new { value = "-300.00"}}
            };
            await ConvertCheckLiquidTemplate(ECRPath, attributes, "\"valueQuantity\": { \"value\": -300.00, },");
        }

        [Fact]
        public async System.Threading.Tasks.Task GivenZeroProperlyReturnsZero()
        {
            var attributes = new Dictionary<string, object>{
                {"value", new { value = "0"}}
            };
            await ConvertCheckLiquidTemplate(ECRPath, attributes, "\"valueQuantity\": { \"value\": 0, },");
        }

        [Fact]
        public async System.Threading.Tasks.Task GivenZeroDecimalProperlyReturnsZeroDecimal()
        {
            var attributes = new Dictionary<string, object>{
                {"value", new { value = "0.0"}}
            };
            await ConvertCheckLiquidTemplate(ECRPath, attributes, "\"valueQuantity\": { \"value\": 0.0, },");
        }

        [Fact]
        public async System.Threading.Tasks.Task GivenValueUnitProperlyReturnsWithValueUnit()
        {
            var attributes = new Dictionary<string, object>{
                {"value", new { value = ".29" , unit = "/d"}}
            };
            await ConvertCheckLiquidTemplate(ECRPath, attributes, "\"valueQuantity\": { \"value\": 0.29, \"unit\":\"/d\", },");
        }

        [Fact]
        public async System.Threading.Tasks.Task GivenNoValueOnlyUnitProperlyReturnsUnit()
        {
            var attributes = new Dictionary<string, object>{
                {"value", new { value = "" , unit = "Immediate"}}
            };
            await ConvertCheckLiquidTemplate(ECRPath, attributes, "\"valueQuantity\": { \"unit\":\"Immediate\", },");
        }
    }
}
