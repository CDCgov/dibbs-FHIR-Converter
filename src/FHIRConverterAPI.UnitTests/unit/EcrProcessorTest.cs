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

  // MergeEicrAndRR

  // def test_add_rr_to_ecr():
  //    with open("./tests/test_files/CDA_RR.xml") as fp:
  //        rr = fp.read()

  //    with open("./tests/test_files/CDA_eICR.xml") as fp:
  //        ecr = fp.read()

  //    # extract rr fields, insert to ecr
  //    ecr = add_rr_data_to_eicr(rr, ecr)

  //    # confirm root tag added
  //    ecr_root = ecr.splitlines()[0]
  //    xsi_tag = "xmlns:xsi"
  //    assert xsi_tag in ecr_root

  //    # confirm new section added
  //    ecr = etree.fromstring(ecr)
  //    tag = "{urn:hl7-org:v3}" + "section"
  //    section = ecr.find(f"./{tag}", namespaces=ecr.nsmap)
  //    assert section is not None

  //    # confirm required elements added
  //    rr_tags = [
  //        "templateId",
  //        "id",
  //        "code",
  //        "title",
  //        "effectiveTime",
  //        "confidentialityCode",
  //        "entry",
  //    ]
  //    rr_tags = ["{urn:hl7-org:v3}" + tag for tag in rr_tags]
  //    for tag in rr_tags:
  //        element = section.find(f"./{tag}", namespaces=section.nsmap)
  //        assert element is not None

  //    # ensure that status has been pulled over
  //    entry_tag = "{urn:hl7-org:v3}" + "entry"
  //    template_id_tag = "{urn:hl7-org:v3}" + "templateId"
  //    code_tag = "{urn:hl7-org:v3}" + "code"
  //    for entry in section.find(f"./{entry_tag}", namespaces=section.nsmap):
  //        for temps in entry.findall(f"./{template_id_tag}", namespaces=entry.nsmap):
  //            status_code = entry.find(f"./{code_tag}", namespaces=entry.nsmap)
  //            assert temps is not None
  //            assert temps.attrib["root"] == "2.16.840.1.113883.10.20.15.2.3.29"
  //            assert "RRVS19" in status_code.attrib["code"]

  [Fact]
  public void MergeEicrAndRR_ShouldAddRRDataToEicr_WhenEicrHasNoRR()
  {
    // TODO
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