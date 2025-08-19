using System.Collections.Generic;
using System.IO;
using DotLiquid;
using Xunit;

namespace Microsoft.Health.Fhir.Liquid.Converter.UnitTests
{
    public class CodingTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "DataType",
            "_Coding.liquid"
        );

        [Fact]
        public void GivenNoAttributeReturnsEmpty()
        {
            var expectedContent = @"""code"": """", ""system"": """",";
            ConvertCheckLiquidTemplate(ECRPath, new Dictionary<string, object>(), expectedContent);
        }
    }
}
