using System.Text.Json.Nodes;
using Microsoft.Health.Fhir.Liquid.Converter.FHIRConverterAPI.Processors;

public class FhirProcessorTest
{
  [Fact]
  public void FhirBundlePostProcessing_ShouldAddSourceToMetaAndReplacePatientId_WhenInputTypeIsProvided()
  {
    var fhirInput = File.ReadAllText("../../../../Microsoft.Health.Fhir.Liquid.Converter.FunctionalTests/TestData/Expected/eCR/EICR/eCR_EveEverywoman-expected.json");

    // Get old patient ID to make sure new one is different
    var inputJson = JsonNode.Parse(fhirInput);
    var inputEntries = inputJson!["entry"] as JsonArray;
    var inputPatientResource = inputEntries!.First(entry => (string)entry!["resource"]!["resourceType"]! == "Patient");
    var oldPatientId = (string)inputPatientResource!["resource"]!["id"]!;

    var actual = FhirProcessor.FhirBundlePostProcessing(fhirInput, "ecr");
    var actualJson = JsonNode.Parse(actual);
    var entries = actualJson!["response"]?["FhirResource"]?["entry"] as JsonArray;
    Assert.True(entries?.Count > 0);

    foreach (var entry in entries)
    {
      Assert.NotNull(entry?["resource"]?["meta"]?["source"]);
      Assert.Equal("ecr", (string)entry!["resource"]!["meta"]!["source"]!);
    }

    var patientResource = entries.First(entry => (string)entry!["resource"]!["resourceType"]! == "Patient");
    Assert.NotEqual(oldPatientId, (string)patientResource!["resource"]!["id"]!);
  }
}