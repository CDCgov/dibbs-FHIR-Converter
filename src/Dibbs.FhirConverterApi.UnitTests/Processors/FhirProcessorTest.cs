using System.Text.Json.Nodes;
using Dibbs.FhirConverterApi.Processors;

namespace Dibbs.FhirConverterApi.UnitTests.Processors;

public class FhirProcessorTest
{
  [Fact]
  public void FhirBundlePostProcessing_ShouldAddSourceToMeta_WhenInputTypeIsProvided()
  {
    var fhirInput = File.ReadAllText("../../../../../data/SampleData/FHIR/eCR_EveEverywoman-expected.json");
    var actual = FhirProcessor.FhirBundlePostProcessing(fhirInput, "ecr");
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