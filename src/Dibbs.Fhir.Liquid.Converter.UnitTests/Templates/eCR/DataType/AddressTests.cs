using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests
{
    public class AddressTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory, "DataType", "Address.liquid"
        );

        [Fact]
        public async Task GivenNoAttributeReturnsEmpty()
        {
            await ConvertCheckLiquidTemplate(ECRPath, new Dictionary<string, object>(), string.Empty);
        }

        [Fact]
        public async Task GivenSingleStreetAddresReturnsLines()
        {
            var attributes = new Dictionary<string, object>{
                {"Address", new { streetAddressLine = new { _ = "132 Main St" }}}
            };
            await ConvertCheckLiquidTemplate(
                ECRPath, 
                attributes, 
                @"""use"": """", ""line"": [""132 Main St"",], ""city"": """", ""state"": """", ""country"": """", ""postalCode"": """", ""district"": """", ""period"": { ""start"":"""", ""end"":"""", },");
        }

        [Fact]
        public async Task GivenArrayStreetAddresReturnsLines()
        {
            var attributes = new Dictionary<string, object>{
                {"Address", new { streetAddressLine = new List<object> {new {_ = "132 Main St"}, new { _ ="Unit 2"} }}}
            };
            await ConvertCheckLiquidTemplate(
                ECRPath, 
                attributes, 
                @"""use"": """", ""line"": [""132 Main St"",""Unit 2"",], ""city"": """", ""state"": """", ""country"": """", ""postalCode"": """", ""district"": """", ""period"": { ""start"":"""", ""end"":"""", },");
        }

        [Fact]
        public async Task GivenCityReturnsCity()
        {
            var attributes = new Dictionary<string, object>{
                {"Address", new { city = new { _ = "Town" }}}
            };
            await ConvertCheckLiquidTemplate(
                ECRPath, 
                attributes, 
                @"""use"": """", ""line"": [], ""city"": ""Town"", ""state"": """", ""country"": """", ""postalCode"": """", ""district"": """", ""period"": { ""start"":"""", ""end"":"""", },");
        }

        [Fact]
        public async Task GivenStateReturnsState()
        {
            var attributes = new Dictionary<string, object>{
                {"Address", new { state = new { _ = "State" }}}
            };
            await ConvertCheckLiquidTemplate(
                ECRPath, 
                attributes, 
                @"""use"": """", ""line"": [], ""city"": """", ""state"": ""State"", ""country"": """", ""postalCode"": """", ""district"": """", ""period"": { ""start"":"""", ""end"":"""", },");
        }

        [Fact]
        public async Task GivenPostalCodeReturnsPostalCode()
        {
            var attributes = new Dictionary<string, object>{
                {"Address", new { postalCode = new { _ = "PostalCode" }}}
            };
            await ConvertCheckLiquidTemplate(
                ECRPath, 
                attributes, 
                @"""use"": """", ""line"": [], ""city"": """", ""state"": """", ""country"": """", ""postalCode"": ""PostalCode"", ""district"": """", ""period"": { ""start"":"""", ""end"":"""", },");
        }

        [Fact]
        public async Task GivenCountyReturnsDistrict()
        {
            var attributes = new Dictionary<string, object>{
                {"Address", new { county = new { _ = "County" }}}
            };
            await ConvertCheckLiquidTemplate(
                ECRPath, 
                attributes, 
                @"""use"": """", ""line"": [], ""city"": """", ""state"": """", ""country"": """", ""postalCode"": """", ""district"": ""County"", ""period"": { ""start"":"""", ""end"":"""", },");
        }

        [Fact]
        public async Task GivenCountrReturnsCountry()
        {
            var attributes = new Dictionary<string, object>{
                {"Address", new { country = new { _ = "Country" }}}
            };
            await ConvertCheckLiquidTemplate(
                ECRPath, 
                attributes, 
                @"""use"": """", ""line"": [], ""city"": """", ""state"": """", ""country"": ""Country"", ""postalCode"": """", ""district"": """", ""period"": { ""start"":"""", ""end"":"""", },");
        }

        [Fact]
        public async Task GivenPeriodReturnsNothing()
        {
            var attributes = new Dictionary<string, object>{
                {"Address", new { useablePeriod = new { low = new { value = "20240313"}} }}
            };
            await ConvertCheckLiquidTemplate(
                ECRPath, 
                attributes, 
                @"");
        }

        [Fact]
        public async Task GivenPeriodAndCountryReturnsBoth()
        {
            var attributes = new Dictionary<string, object>{
                {"Address", new { country = new {_ = "Country" }, useablePeriod = new { low = new { value = "20240313"}} }}
            };
            await ConvertCheckLiquidTemplate(
                ECRPath, 
                attributes, 
                @"""use"": """", ""line"": [], ""city"": """", ""state"": """", ""country"": ""Country"", ""postalCode"": """", ""district"": """", ""period"": { ""start"":""2024-03-13"", ""end"":"""", },");
        }

        [Fact]
        public async System.Threading.Tasks.Task EscapesSpecialChars()
        {
            var attributes = new Dictionary<string, object>{
                {"Address", 
                    new { 
                        streetAddressLine = new [] { 
                            new {_ = @"Li\ne1" }, 
                            new { _ = @"""Line2""" }
                        }, 
                        city = new {_ = "/" },
                        state = new {_ = @"\" },
                        country = new {_ = "\"\"" },
                        postalCode = new {_ = "[12345]" },
                        county = new {_ = @"Dis\trict" },
                        useablePeriod = new { 
                            low = new { value = "20240313"}
                        } 
                    }
                }
            };

            await ConvertCheckLiquidTemplate(
                ECRPath, 
                attributes, 
                @"""use"": """", ""line"": [""Li\\ne1"",""\""Line2\"""",], ""city"": ""/"", ""state"": ""\\"", ""country"": ""\""\"""", ""postalCode"": ""[12345]"", ""district"": ""Dis\\trict"", ""period"": { ""start"":""2024-03-13"", ""end"":"""", },");
        }

    }
   
}
