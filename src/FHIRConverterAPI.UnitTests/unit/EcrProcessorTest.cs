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
    // TODO: Failing due to duplicate xmlns xsi
    var input = "<ClinicalDocument xmlns=\"urn:hl7-org:v3\" xmlns:sdtc=\"urn:hl7-org:sdtc\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"><component><structuredBody><component><section><text><content ID=\"birthsex\">Female</content><content ID=\"gender-identity\">unknown</content></text><entry><observation classCode=\"OBS\" moodCode=\"EVN\"><reference value=\"#birthsex\"/></observation></entry><entry><observation classCode=\"OBS\" moodCode=\"EVN\"><reference value=\"#gender-identity\"/></observation></entry></section></component></structuredBody></component></ClinicalDocument>";
    var actual = EcrProcessor.ConvertStringToXDocument(input);
    var elements = actual.Elements().ToArray();

    Assert.Equal("#birthsex", elements?[0]?.Attribute("value")?.Value);
    Assert.Equal("Female", elements?[0].Value);
    Assert.Equal("#gender-identity", elements?[1]?.Attribute("value")?.Value);
    Assert.Equal("unknown", elements?[1].Value);
  }

  // MergeEicrAndRR

  // def test_resolve_references_valid_input():
  //    tree = etree.fromstring(resolve_references(bundle_with_references))
  //    actual_refs = tree.xpath("//hl7:reference", namespaces={"hl7": "urn:hl7-org:v3"})
  //    assert actual_refs[0].attrib["value"] == "#birthsex"
  //    assert actual_refs[0].text == "Female"
  //    assert actual_refs[1].attrib["value"] == "#gender-identity"
  //    assert actual_refs[1].text == "unknown"

  // def test_resolve_references_invalid_input():
  //    actual = resolve_references("VXU or HL7 MESSAGE")
  //    assert actual == "VXU or HL7 MESSAGE"

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

  // def test_add_rr_to_ecr_rr_already_present(capfd):
  //    with open("./tests/test_files/CDA_RR.xml") as fp:
  //        rr = fp.read()

  //    # This eICR has already been merged with an RR
  //    with open("./tests/test_files/merged_eICR.xml") as fp:
  //        ecr = fp.read()

  //    merged_ecr = add_rr_data_to_eicr(rr, ecr)
  //    assert merged_ecr == ecr

  //    out, err = capfd.readouterr()
  //    assert "This eCR has already been merged with RR data." in out

  // def test_add_rr_to_ecr_rr_remove_extra_rr(capfd):
  //    with open("./tests/test_files/3.1/CDA_eICR.xml") as fp:
  //        eicr = fp.read()
  //    with open("./tests/test_files/3.1/CDA_RR.xml") as fp:
  //        rr = fp.read()

  //    ecr = add_rr_data_to_eicr(rr, eicr)
  //    root = etree.fromstring(ecr)

  //    # RR section ("88085-6") should appear only once
  //    code_RR_loinc = root.xpath(
  //        './/hl7:*[@code="88085-6"]', namespaces={"hl7": "urn:hl7-org:v3"}
  //    )
  //    assert len(code_RR_loinc) == 1

  //    # RR section from eICR (v>3.1) should not exist (should have been removed)
  //    rr_from_eicr = root.xpath(
  //        './/hl7:templateId[@root="2.16.840.1.113883.10.20.15.2.2.5" and @extension="2021-01-01"]',
  //        namespaces={"hl7": "urn:hl7-org:v3"},
  //    )
  //    assert len(rr_from_eicr) == 0
}