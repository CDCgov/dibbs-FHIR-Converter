using System.Text.Json.Nodes;
using Dibbs.FhirConverterApi.Processors;

namespace Dibbs.FhirConverterApi.UnitTests.Processors;

public class FhirProcessorTest
{
  [Fact]
  public void ConvertXmlToJson_ReturnsEquivalentFhirJson_WhenInputIsFhirXmlBundle()
  {
    const string fhirXml = """
      <Bundle xmlns="http://hl7.org/fhir">
        <id value="bundle-example" />
        <identifier>
          <system value="urn:ietf:rfc:3986" />
          <value value="urn:uuid:12345678-1234-1234-1234-123456789abc" />
        </identifier>
        <type value="collection" />
        <entry>
          <fullUrl value="urn:uuid:87654321-4321-4321-4321-cba987654321" />
          <resource>
            <Patient>
              <id value="patient-example" />
            </Patient>
          </resource>
        </entry>
      </Bundle>
      """;

    var actual = FhirProcessor.ConvertXmlToJson(fhirXml);
    var bundle = JsonNode.Parse(actual);

    Assert.Equal("Bundle", (string)bundle!["resourceType"] !);
    Assert.Equal("bundle-example", (string)bundle["id"] !);
    Assert.Equal(
      "urn:uuid:12345678-1234-1234-1234-123456789abc",
      (string)bundle["identifier"] !["value"] !);
    Assert.Equal("Patient", (string)bundle["entry"] ![0] !["resource"] !["resourceType"] !);
  }

  [Fact]
  public void ConvertXmlToJson_Throws_WhenInputIsNotValidFhirXml()
  {
    const string invalidFhirXml = """
      <Bundle xmlns="http://hl7.org/fhir">
        <notAField value="invalid" />
      </Bundle>
      """;

    var exception = Assert.Throws<Models.UserFacingException>(
      () => FhirProcessor.ConvertXmlToJson(invalidFhirXml));

    Assert.Equal("FHIR XML input must be a valid FHIR R4 Bundle.", exception.Message);
  }

  [Fact]
  public void FhirBundlePostProcessing_ShouldAddSourceToMeta_WhenInputTypeIsProvided()
  {
    var fhirInput = File.ReadAllText("../../../../../data/SampleData/FHIR/eCR_EveEverywoman-expected.json");
    var actual = FhirProcessor.FhirBundlePostProcessing(fhirInput);
    var actualJson = JsonNode.Parse(actual);
    var entries = actualJson!["response"]?["FhirResource"]?["entry"] as JsonArray;
    Assert.True(entries?.Count > 0);

    foreach (var entry in entries)
    {
      Assert.NotNull(entry?["resource"]?["meta"]?["source"]);
      Assert.Equal("ecr", (string)entry!["resource"] !["meta"] !["source"] !);
    }
  }
}
