using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.Health.Fhir.Liquid.Converter.FHIRConverterAPI.Processors;

public class EcrProcessorTest
{
  [Fact]
  public void ConvertStringToXDocument_ShouldAddXSI_WhenItIsMissing()
  {
    var input = "<ClinicalDocument xmlns=\"urn:hl7-org:v3\"></ClinicalDocument>";
    var actual = EcrProcessor.ConvertStringToXDocument(input);
    Assert.Contains("xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"", actual.ToString());
  }

  [Fact]
  public void ConvertStringToXDocument_ResolvesReferences_WhenTheyExist()
  {
    var input = "<ClinicalDocument xmlns=\"urn:hl7-org:v3\" xmlns:sdtc=\"urn:hl7-org:sdtc\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"><component><structuredBody><component><section><text><content ID=\"birthsex\">Female</content><content ID=\"gender-identity\">unknown</content></text><entry><observation classCode=\"OBS\" moodCode=\"EVN\"><reference value=\"#birthsex\"/></observation></entry><entry><observation classCode=\"OBS\" moodCode=\"EVN\"><reference value=\"#gender-identity\"/></observation></entry></section></component></structuredBody></component></ClinicalDocument>";
    var actual = EcrProcessor.ConvertStringToXDocument(input);
    var names = new XmlNamespaceManager(actual.CreateNavigator().NameTable);
    names.AddNamespace("hl7", "urn:hl7-org:v3");
    var entries = actual.XPathSelectElements(
              "//hl7:component/hl7:structuredBody/hl7:component/hl7:section/hl7:entry",
              names);

    Assert.Equal("#birthsex", entries?.ElementAt(0)?.XPathSelectElement("hl7:observation/hl7:reference", names)?.Attribute("value")?.Value);
    Assert.Equal("Female", entries?.ElementAt(0).Value);
    Assert.Equal("#gender-identity", entries?.ElementAt(1)?.XPathSelectElement("hl7:observation/hl7:reference", names)?.Attribute("value")?.Value);
    Assert.Equal("unknown", entries?.ElementAt(1).Value);
  }

  [Fact]
  public void MergeEicrAndRR_ShouldAddRRDataToEicr_WhenEicrHasNoRR()
  {
    var rr = File.ReadAllText("../../../TestData/CDA_RR.xml");
    var ecr = File.ReadAllText("../../../TestData/CDA_eICR.xml");
    var ecrXDoc = XDocument.Parse(ecr);
    var mergedEcr = EcrProcessor.MergeEicrAndRR(ecrXDoc, rr);
    var names = new XmlNamespaceManager(mergedEcr.CreateNavigator().NameTable);
    names.AddNamespace("hl7", "urn:hl7-org:v3");
    var section = mergedEcr.Root!.XPathSelectElement("hl7:section", names);
    Assert.NotNull(section);

    string[] rrTags = [
          "templateId",
          "id",
          "code",
          "title",
          "effectiveTime",
          "confidentialityCode",
          "entry",
      ];

    foreach (var tag in rrTags)
    {
      var element = section.XPathSelectElement($"hl7:{tag}", names);
      Assert.NotNull(element);
    }

    var entry = section.XPathSelectElement("hl7:entry/hl7:act", names);
    Assert.NotNull(entry);

    var statusCode = entry.XPathSelectElement("hl7:code", names);
    Assert.NotNull(statusCode);
    Assert.Contains("RRVS19", statusCode.Attribute("code")?.Value);

    foreach (var temps in entry.XPathSelectElements("hl7:templateId", names))
    {
      Assert.NotNull(temps);
      Assert.Equal("2.16.840.1.113883.10.20.15.2.3.29", temps.Attribute("root")?.Value);
    }
  }

  [Fact]
  public void MergeEicrAndRR_ShouldNotAddRRDataToEicr_WhenEicrHasRR()
  {
    var rr = File.ReadAllText("../../../TestData/CDA_RR.xml");
    var ecr = File.ReadAllText("../../../TestData/merged_eICR.xml");
    var ecrXDoc = XDocument.Parse(ecr);
    var mergedEcr = EcrProcessor.MergeEicrAndRR(ecrXDoc, rr);

    Assert.Equal(ecrXDoc.ToString(), mergedEcr.ToString());
  }

  [Fact]
  public void MergeEicrAndRR_ShouldRemoveExtraRR_WhenEicrIs3_1()
  {
    var ecr = File.ReadAllText("../../../TestData/3.1_CDA_eICR.xml");
    var ecrXDoc = XDocument.Parse(ecr);
    var rr = File.ReadAllText("../../../TestData/3.1_CDA_RR.xml");

    var mergedEcr = EcrProcessor.MergeEicrAndRR(ecrXDoc, rr);

    var names = new XmlNamespaceManager(mergedEcr.CreateNavigator().NameTable);
    names.AddNamespace("hl7", "urn:hl7-org:v3");

    var codeRrLoinc = mergedEcr.XPathSelectElements(".//hl7:*[@code='88085-6']", names);
    Assert.Single(codeRrLoinc);

    var rrFromEicr = mergedEcr.XPathSelectElements(".//hl7:templateId[@root='2.16.840.1.113883.10.20.15.2.2.5' and @extension='2021-01-01']", names);
    Assert.Empty(rrFromEicr);
  }
}