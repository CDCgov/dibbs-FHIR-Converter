using System.Collections.Generic;
using System.IO;
using Dibbs.Fhir.Liquid.Converter.DataParsers;
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
            await ConvertCheckLiquidTemplate(ECRPath, new Dictionary<string, object>(), "\"valueString\": \"\",");
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
        public async System.Threading.Tasks.Task GivenValueUnitProperlyReturnsWithValueUnit()
        {
            var attributes = new Dictionary<string, object>{
                {"value", new { value = ".29" , unit = "/d"}}
            };
            await ConvertCheckLiquidTemplate(ECRPath, attributes, "\"valueQuantity\": { \"value\": 0.29, \"unit\": \"/d\", },");
        }

        [Fact]
        public async System.Threading.Tasks.Task GivenNoValueOnlyUnitProperlyReturnsUnit()
        {
            var attributes = new Dictionary<string, object>{
                {"value", new { value = "" , unit = "Immediate"}}
            };
            await ConvertCheckLiquidTemplate(ECRPath, attributes, "\"valueQuantity\": { \"unit\": \"Immediate\", },");
        }

        [Fact]
        public async System.Threading.Tasks.Task GivenUnitInValueStringProperlyReturnsWithValueUnit()
        {
            var attributes = new Dictionary<string, object>{
                {"value", new { value = "5mg" }}
            };
            await ConvertCheckLiquidTemplate(ECRPath, attributes, "\"valueQuantity\": { \"value\": 5, \"unit\": \"mg\", },");
        }

        [Fact]
        public async System.Threading.Tasks.Task GivenRefInnerTextProperlyReturnsText()
        {
            var attributes = new Dictionary<string, object>{
                {"value", new { originalText = new { reference = new { _ = "some value" }}}}
            };
            await ConvertCheckLiquidTemplate(ECRPath, attributes, "\"valueString\": \"some value\",");
        }

        [Fact]
        public async System.Threading.Tasks.Task GivenValueInnerTextProperlyReturnsText()
        {
            var attributes = new Dictionary<string, object>{
                {"value", new { _ = "some value" }}
            };
            await ConvertCheckLiquidTemplate(ECRPath, attributes, "\"valueString\": \"some value\",");
        }

        [Fact]
        public async System.Threading.Tasks.Task GivenOriginalTextInnerTextProperlyReturnsText()
        {
            var attributes = new Dictionary<string, object>{
                {"value", new { originalText = new { _ = "some value" }}}
            };
            await ConvertCheckLiquidTemplate(ECRPath, attributes, "\"valueString\": \"some value\",");
        }

        [Fact]
        public async System.Threading.Tasks.Task GivenBooleanProperlyReturnsValueBoolean()
        {
            var xmlStr = @"<value xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""BL"" value=""true"" />";
            var attributes = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;
            await ConvertCheckLiquidTemplate(ECRPath, attributes, "\"valueBoolean\": true,");
        }

        [Fact]
        public async System.Threading.Tasks.Task GivenIntegerProperlyReturnsValueInteger()
        {
            var xmlStr = @"<value xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""INT"" value=""20"" />";
            var attributes = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;
            await ConvertCheckLiquidTemplate(ECRPath, attributes, "\"valueInteger\": 20,");
        }

        [Fact]
        public async System.Threading.Tasks.Task GivenTimestampProperlyReturnsValueDateTime()
        {
            var xmlStr = @"<value xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:type=""TS"" value=""20260522"" />";
            var attributes = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;
            await ConvertCheckLiquidTemplate(ECRPath, attributes, "\"valueDateTime\": \"2026-05-22\",");
        }

        [Fact]
        public async System.Threading.Tasks.Task GivenTranslation()
        {
            var xmlStr = """
            <value xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" unit="Immediate" value="" xsi:type="PQ">
                <translation code="88694003" codeSystem="2.16.840.1.113883.6.96" codeSystemName="SNOMED CT" displayName="Immediate (qualifier value)" />
            </value>
            """;

            var attributes = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;

            await ConvertCheckLiquidTemplate(ECRPath, attributes, "\"valueQuantity\": { \"unit\": \"Immediate\", \"extension\": [ { \"url\": \"http://hl7.org/fhir/StructureDefinition/extension-quantity-translation\", \"valueQuantity\": {\"unit\": \"Immediate (qualifier value)\",\"system\": \"http://snomed.info/sct\", \"code\": \"88694003\" } } ] },");
        }

        [Fact]
        public async System.Threading.Tasks.Task GivenValueCodeSingleValuedProperlyReturnsWithValueCoding()
        {
            var attributes = new Dictionary<string, object>{
                {"value", new { code = "1234" , displayName = "some coding", codeSystem = "a code system"}},
                { "singleValued", true }
            };
            await ConvertCheckLiquidTemplate(ECRPath, attributes, "\"valueCoding\": { \"code\": \"1234\",\"system\": \"a code system\",\"display\": \"some coding\", },");
        }

        [Fact]
        public async System.Threading.Tasks.Task GivenValueCodeProperlyReturnsWithValueCodeableConcept()
        {
            var attributes = new Dictionary<string, object>{
                {"value", new { code = "1234" , displayName = "some coding", codeSystem = "a code system"}}
            };
            await ConvertCheckLiquidTemplate(ECRPath, attributes, "\"valueCodeableConcept\": { \"coding\": [ { \"code\": \"1234\",\"system\": \"a code system\",\"display\": \"some coding\",},],},");
        }
    }
}
