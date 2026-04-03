using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests
{
    public class ContactPointTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory, "DataType", "ContactPoint.liquid"
        );

        [Fact]
        public async System.Threading.Tasks.Task GivenNoAttributeReturnsEmpty()
        {
            await ConvertCheckLiquidTemplate(ECRPath, new Dictionary<string, object>(), string.Empty);
        }

        [Fact]
        public async System.Threading.Tasks.Task GivenTelValueReturnsPhone()
        {
            var attributes = new Dictionary<string, object>{
                {"ContactPoint", new { value = "tel:123" }}
            };
            await ConvertCheckLiquidTemplate(
                ECRPath, 
                attributes, 
                @"""system"":""phone"", ""value"": ""123"", ""use"": """",");
        }

        [Fact]
        public async System.Threading.Tasks.Task GivenTelValuAndUseReturnsPhone()
        {
            var attributes = new Dictionary<string, object>{
                {"ContactPoint", new { value = "tel:123", use="H" }}
            };
            await ConvertCheckLiquidTemplate(
                ECRPath, 
                attributes, 
                @"""system"":""phone"", ""value"": ""123"", ""use"": ""home"",");
        }

        [Fact]
        public async System.Threading.Tasks.Task GivenTelValuAndPagerUseReturnsPager()
        {
            var attributes = new Dictionary<string, object>{
                {"ContactPoint", new { value = "tel:123", use="PG" }}
            };
            await ConvertCheckLiquidTemplate(
                ECRPath, 
                attributes, 
                @"""system"":""pager"", ""value"": ""123"",");
        }

        [Fact]
        public async System.Threading.Tasks.Task GivenMailtoReturnsEmail()
        {
            var attributes = new Dictionary<string, object>{
                {"ContactPoint", new { value = "mailto:abc@me.com", use="WP" }}
            };
            await ConvertCheckLiquidTemplate(
                ECRPath, 
                attributes, 
                @"""system"":""email"", ""value"": ""abc@me.com"", ""use"": ""work"",");
        }

        [Fact]
        public async System.Threading.Tasks.Task GivenFaxoReturnsFax()
        {
            var attributes = new Dictionary<string, object>{
                {"ContactPoint", new { value = "fax:123", use="WP" }}
            };
            await ConvertCheckLiquidTemplate(
                ECRPath, 
                attributes, 
                @"""system"":""fax"", ""value"": ""123"", ""use"": ""work"",");
        }

        [Fact]
        public async System.Threading.Tasks.Task GivenTelecomWithNoPrefixReturnsValueString()
        {
            var attributes = new Dictionary<string, object>{
                {"ContactPoint", new { value = "123", use="WP" }}
            };
            await ConvertCheckLiquidTemplate(
                ECRPath,
                attributes,
                @"""value"": ""123"", ""use"": ""work"",");
        }
    }
   
}
