using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Dibbs.FhirConverterApi.Processors;

namespace Dibbs.FhirConverterApi.UnitTests.Processors;

public class EcrProcessorTest
{
  [Fact]
  public void ResolveReferences_ResolvesReferences_WhenTheyExist()
  {
    var input = @"
      <ClinicalDocument xmlns=""urn:hl7-org:v3"" xmlns:sdtc=""urn:hl7-org:sdtc"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
        <component>
          <structuredBody>
            <component>
              <section>
                <text>
                  <content ID=""birthsex"">Female</content>
                  <content ID=""gender-identity"">unknown</content>
                </text>
                <entry>
                  <observation classCode=""OBS"" moodCode=""EVN"">
                    <reference value=""#birthsex""/>
                  </observation>
                </entry>
                <entry>
                  <observation classCode=""OBS"" moodCode=""EVN"">
                    <reference value=""#gender-identity""/>
                  </observation>
                </entry>
              </section>
            </component>
          </structuredBody>
        </component>
      </ClinicalDocument>";

    var inputXDoc = XDocument.Parse(input);
    var actual = EcrProcessor.ResolveReferences(inputXDoc);
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
  public void ResolveReferences_PreservesInnerTags_WhenResolvingReferences()
  {
    var input = @"
      <ClinicalDocument xmlns=""urn:hl7-org:v3"" xmlns:sdtc=""urn:hl7-org:sdtc"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
        <component>
          <structuredBody>
            <component>
              <section>
                <text>
                  <content ID=""trvhx-1"">Traveled to Singapore, Malaysia and Bali with<br />my family.</content>
                </text>
                <entry>
                  <observation classCode=""OBS"" moodCode=""EVN"">
                    <reference value=""#trvhx-1""/>
                  </observation>
                </entry>
              </section>
            </component>
          </structuredBody>
        </component>
      </ClinicalDocument>";

    var inputXDoc = XDocument.Parse(input);
    var actual = EcrProcessor.ResolveReferences(inputXDoc);
    var names = new XmlNamespaceManager(actual.CreateNavigator().NameTable);
    names.AddNamespace("hl7", "urn:hl7-org:v3");
    var entries = actual.XPathSelectElements(
              "//hl7:component/hl7:structuredBody/hl7:component/hl7:section/hl7:entry",
              names);

    Assert.Equal("#trvhx-1", entries?.ElementAt(0)?.XPathSelectElement("hl7:observation/hl7:reference", names)?.Attribute("value")?.Value);
    Assert.Equal("Traveled to Singapore, Malaysia and Bali with<br xmlns=\"urn:hl7-org:v3\" />my family.", entries?.ElementAt(0).Value);
  }

  [Fact]
  public void MergeEicrAndRR_ShouldAddRRDataToEicr_WhenEicrHasNoRR()
  {
    var rr = File.ReadAllText("../../../../../data/SampleData/eCR/RR_EveEverywoman.xml");
    var ecr = File.ReadAllText("../../../../../data/SampleData/eCR/empty_eICR.xml");
    var ecrXDoc = XDocument.Parse(ecr);
    var mergedEcr = EcrProcessor.MergeEicrAndRR(ecrXDoc, rr);
    var names = new XmlNamespaceManager(mergedEcr.CreateNavigator().NameTable);
    names.AddNamespace("hl7", "urn:hl7-org:v3");
    var section = mergedEcr.Root!.XPathSelectElement("hl7:section", names);
    Assert.NotNull(section);

    string[] rrTags =
    [
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
    var rr = File.ReadAllText("../../../../../data/SampleData/eCR/RR_EveEverywoman.xml");
    var ecr = File.ReadAllText("../../../../../data/SampleData/eCR/merged_eICR.xml");
    var ecrXDoc = XDocument.Parse(ecr);
    var mergedEcr = EcrProcessor.MergeEicrAndRR(ecrXDoc, rr);

    Assert.Equal(ecrXDoc.ToString(), mergedEcr.ToString());
  }

  [Fact]
  public void MergeEicrAndRR_ShouldRemoveExtraRR_WhenEicrIs3_1()
  {
    var ecr = File.ReadAllText("../../../../../data/SampleData/eCR/3.1_CDA_eICR.xml");
    var ecrXDoc = XDocument.Parse(ecr);
    var rr = File.ReadAllText("../../../../../data/SampleData/eCR/3.1_CDA_RR.xml");

    var mergedEcr = EcrProcessor.MergeEicrAndRR(ecrXDoc, rr);

    var names = new XmlNamespaceManager(mergedEcr.CreateNavigator().NameTable);
    names.AddNamespace("hl7", "urn:hl7-org:v3");

    var codeRrLoinc = mergedEcr.XPathSelectElements(".//hl7:*[@code='88085-6']", names);
    Assert.Single(codeRrLoinc);

    var rrFromEicr = mergedEcr.XPathSelectElements(".//hl7:templateId[@root='2.16.840.1.113883.10.20.15.2.2.5' and @extension='2021-01-01']", names);
    Assert.Empty(rrFromEicr);
  }

  [Fact]
  public void ResolveEntryReferences_ReplacesReferenceActWithReferencedStatement()
  {
    var document = @"<ClinicalDocument xmlns=""urn:hl7-org:v3"">
                             <entry>
                               <observation classCode=""OBS"" moodCode=""EVN"">
                                 <templateId root=""wrong-template""/>
                                 <id root=""shared-root"" extension=""wrong-extension""/>
                                 <code code=""wrong-code""/>
                               </observation>
                             </entry>
                             <entry>
                               <observation classCode=""OBS"" moodCode=""EVN"">
                                 <templateId root=""target-template""/>
                                 <id root=""shared-root"" extension=""target-extension""/>
                                 <code code=""target-code""/>
                               </observation>
                             </entry>
                             <entry>
                               <act classCode=""ACT"" moodCode=""EVN"">
                                 <templateId root=""2.16.840.1.113883.10.20.22.4.122""/>
                                 <id root=""shared-root"" extension=""target-extension""/>
                                 <code nullFlavor=""NP""/>
                                 <statusCode code=""completed""/>
                               </act>
                             </entry>
                           </ClinicalDocument>";
    var actual = EcrProcessor.ResolveEntryReferences(XDocument.Parse(document));
    XNamespace cda = "urn:hl7-org:v3";
    var entries = actual.Root!.Elements(cda + "entry").ToList();
    var referenceEntry = entries[2];

    Assert.Null(referenceEntry.Element(cda + "act"));

    var observation = Assert.Single(referenceEntry.Elements(cda + "observation"));
    var templateId = Assert.Single(observation.Elements(cda + "templateId"));
    var code = Assert.Single(observation.Elements(cda + "code"));

    Assert.Equal("target-template", templateId.Attribute("root")?.Value);
    Assert.Equal("target-code", code.Attribute("code")?.Value);
  }

  [Fact]
  public void ResolveEntryReferences_ReplacesReferenceActInsideEntryRelationship()
  {
    var document = @"<ClinicalDocument xmlns=""urn:hl7-org:v3"">
                             <entry>
                               <observation classCode=""OBS"" moodCode=""EVN"">
                                 <id root=""parent-observation""/>
                                 <entryRelationship typeCode=""REFR"">
                                   <act classCode=""ACT"" moodCode=""EVN"">
                                     <templateId root=""2.16.840.1.113883.10.20.22.4.122""/>
                                     <id root=""target-procedure""/>
                                     <code nullFlavor=""NP""/>
                                     <statusCode code=""completed""/>
                                   </act>
                                 </entryRelationship>
                               </observation>
                             </entry>
                             <entry>
                               <procedure classCode=""PROC"" moodCode=""EVN"">
                                 <templateId root=""target-procedure-template""/>
                                 <id root=""target-procedure""/>
                                 <code code=""target-procedure-code""/>
                               </procedure>
                             </entry>
                           </ClinicalDocument>";
    var actual = EcrProcessor.ResolveEntryReferences(XDocument.Parse(document));
    XNamespace cda = "urn:hl7-org:v3";
    var parentEntry = actual.Root!.Elements(cda + "entry").First();
    var parentObservation = Assert.Single(parentEntry.Elements(cda + "observation"));
    var entryRelationship = Assert.Single(parentObservation.Elements(cda + "entryRelationship"));

    Assert.Equal("REFR", entryRelationship.Attribute("typeCode")?.Value);
    Assert.Null(entryRelationship.Element(cda + "act"));

    var procedure = Assert.Single(entryRelationship.Elements(cda + "procedure"));
    var templateId = Assert.Single(procedure.Elements(cda + "templateId"));
    var code = Assert.Single(procedure.Elements(cda + "code"));

    Assert.Equal("target-procedure-template", templateId.Attribute("root")?.Value);
    Assert.Equal("target-procedure-code", code.Attribute("code")?.Value);
  }

  [Fact]
  public void ResolveEntryReferences_PreservesUnmatchedReferenceAct()
  {
    var document = @"<ClinicalDocument xmlns=""urn:hl7-org:v3"">
                             <entry>
                               <act classCode=""ACT"" moodCode=""EVN"">
                                 <templateId root=""2.16.840.1.113883.10.20.22.4.122""/>
                                 <id root=""missing-target""/>
                                 <code nullFlavor=""NP""/>
                                 <statusCode code=""completed""/>
                               </act>
                             </entry>
                           </ClinicalDocument>";
    var actual = EcrProcessor.ResolveEntryReferences(XDocument.Parse(document));
    XNamespace cda = "urn:hl7-org:v3";
    var entry = Assert.Single(actual.Root!.Elements(cda + "entry"));
    var act = Assert.Single(entry.Elements(cda + "act"));
    var templateId = Assert.Single(act.Elements(cda + "templateId"));

    Assert.Equal("2.16.840.1.113883.10.20.22.4.122", templateId.Attribute("root")?.Value);
  }
}
