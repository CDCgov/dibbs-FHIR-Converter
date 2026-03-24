using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests
{
    public class HumanNameTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory, "DataType", "HumanName.liquid"
        );

        [Fact]
        public async Task ConvertsGivenNames()
        {
            var attributes = new Dictionary<string, object>{
                {"HumanName", new { given = new [] { new { _ = "GivenName1" }, new { _ = "GivenName2" } }}}
            };
            await ConvertCheckLiquidTemplate(
                ECRPath, 
                attributes,
                @"""use"":"""", ""family"":"""", ""given"":[ ""GivenName1"", ""GivenName2"", ], ""prefix"": [ """", ], ""suffix"": [ """", ]");
        }

        [Fact]
        public async Task ConvertsFamilyName()
        {
            var attributes = new Dictionary<string, object>{
                {"HumanName", new { family = new { _ = "FamilyName" } }}
            };
            await ConvertCheckLiquidTemplate(
                ECRPath, 
                attributes,
                @"""use"":"""", ""family"":""FamilyName"", ""given"":[ ], ""prefix"": [ """", ], ""suffix"": [ """", ]");
        }

        [Fact]
        public async Task ConvertsPrefix()
        {
            var attributes = new Dictionary<string, object>{
                {"HumanName", new { prefix = new { _ = "Prefix." } }}
            };
            await ConvertCheckLiquidTemplate(
                ECRPath, 
                attributes,
                @"""use"":"""", ""family"":"""", ""given"":[ ], ""prefix"": [ ""Prefix."", ], ""suffix"": [ """", ]");
        }

        [Fact]
        public async Task ConvertsSuffix()
        {
            var attributes = new Dictionary<string, object>{
                {"HumanName", new { suffix = new { _ = "Suffix." } }}
            };
            await ConvertCheckLiquidTemplate(
                ECRPath, 
                attributes,
                @"""use"":"""", ""family"":"""", ""given"":[ ], ""prefix"": [ """", ], ""suffix"": [ ""Suffix."", ]");
        }

        [Fact]
        public async Task EscapesSpecialCharacters()
        {
            var attributes = new Dictionary<string, object>{
                {"HumanName", new { family = new { _ = @"FamilyName\" }, given = new [] { new { _ = @"Given\Name1" }, new { _ = @"GivenName\2" } }, prefix = new { _ = @"Prefix\." }, suffix = new { _ = @"""Suffix.""" }}}
            };
            await ConvertCheckLiquidTemplate(
                ECRPath, 
                attributes,
                @"""use"":"""", ""family"":""FamilyName\\"", ""given"":[ ""Given\\Name1"", ""GivenName\\2"", ], ""prefix"": [ ""Prefix\\."", ], ""suffix"": [ ""\""Suffix.\"""", ]");
        }
    }
   
}
