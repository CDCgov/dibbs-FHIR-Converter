using System.Net;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Microsoft.Health.Fhir.Liquid.Converter.FHIRConverterAPI.Processors
{
  public class EcrProcessor
  {
    /// <summary>
    ///  Adds XML Schema Instance if it is missing from document
    ///  then converts document from string to XDocument.
    /// </summary>
    /// <param name="inputData">A serialized xml format electronic initial case report (eICR) document.</param>
    /// <returns>An XDocument object containing an eICR.</returns>
    public static XDocument ConvertStringToXDocument(string inputData)
    {
      try
      {
        // Add xmlns:xsi if missing
        // TODO: this seems fragile, do we need to add xmlns:xsi to the raw string or can we parse to XDocument first?
        var ecrLines = inputData.Split(
            ["\n", "\r\n"],
            StringSplitOptions.None).ToList();
        var startIndex = ecrLines.FindIndex(line => line.TrimStart().StartsWith("<ClinicalDocument"));
        var endIndex = ecrLines.FindIndex(startIndex, line => line.TrimEnd().EndsWith('>'));

        if (ecrLines.FindIndex(startIndex, endIndex, line => line.Contains("xmlns:xsi")) == -1)
        {
          var newRootElement = string.Join(" ", ecrLines.ToArray(), startIndex, endIndex - startIndex + 1);
          newRootElement = newRootElement.Replace("xmlns=\"urn:hl7-org:v3\"", "xmlns=\"urn:hl7-org:v3\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"");
          ecrLines.RemoveRange(startIndex, endIndex - startIndex + 1);
          ecrLines.Insert(startIndex, newRootElement);
          inputData = string.Join("\n", ecrLines.ToArray());
        }

        var ecrDoc = XDocument.Parse(inputData);
        return ResolveReferences(ecrDoc);
      }
      catch (Exception ex)
      {
        throw new UserFacingException("EICR message must be valid XML message.", HttpStatusCode.UnprocessableEntity, ex);
      }
    }

    /// <summary>
    ///  Extracts relevant fields from an RR document, and inserts them into a
    ///  given eICR document. Ensures that the eICR contains properly formatted
    ///  RR fields, including templateId, id, code, title, effectiveTime,
    ///  confidentialityCode, and corresponding entries; and required format tags.
    /// </summary>
    /// <param name="ecrXDocument">An XDocument object containing an electronic initial case report (eICR).</param>
    /// <param name="rrData">A serialized xml format reportability response (RR) document.</param>
    /// <returns>An xml format eICR document with additional fields extracted from the RR.</returns>
    public static XDocument MergeEicrAndRR(XDocument ecrXDocument, string rrData)
    {
      XDocument rrXDocument;

      try
      {
        rrXDocument = XDocument.Parse(rrData);
      }
      catch (Exception ex)
      {
        throw new UserFacingException("Reportability Response (RR) message must be valid XML message.", HttpStatusCode.UnprocessableEntity, ex);
      }

      try
      {
        // Check for eICR Processing Status entry (required & only available in RR)
        if (ecrXDocument.XPathSelectElement("//*[@root=\"2.16.840.1.113883.10.20.15.2.3.29\"]") is not null)
        {
          Console.WriteLine("This eCR has already been merged with RR data.");
          return ecrXDocument;
        }

        // If eICR >=R3, remove (optional) RR section that came from eICR
        // This is duplicate/incomplete info from RR
        var ecrVersion = ecrXDocument.XPathEvaluate("string(//*[@root=\"2.16.840.1.113883.10.20.15.2\"]/@extension)")?.ToString();
        if (!string.IsNullOrEmpty(ecrVersion) && DateTime.Parse(ecrVersion.ToString()) >= DateTime.Parse("2021-01-01"))
        {
          var names = new XmlNamespaceManager(ecrXDocument.CreateNavigator().NameTable);
          names.AddNamespace("hl7", "urn:hl7-org:v3");
          var rrFromEicr = ecrXDocument.XPathSelectElement(
              "//hl7:component[hl7:section/hl7:templateId[@root=\"2.16.840.1.113883.10.20.15.2.2.5\"]]",
              names);

          rrFromEicr?.Remove();
        }

        // Create the tags for elements we'll be looking for
        var rrTags = new string[]
        {
            "templateId",
            "id",
            "code",
            "title",
            "effectiveTime",
            "confidentialityCode",
        };

        var rrElements = new List<XElement>();
        var rrNames = new XmlNamespaceManager(rrXDocument.CreateNavigator().NameTable);
        rrNames.AddNamespace("hl7", "urn:hl7-org:v3");

        // Find root-level elements and add them to a list
        foreach (var tag in rrTags)
        {
          var element = rrXDocument.XPathSelectElement($"/*/hl7:{tag}", rrNames);
          if (element is not null)
          {
            rrElements.Add(element);
          }
        }

        // Find the nested entry element that we need
        var rrEntry = rrXDocument.XPathSelectElement(
            "/*/hl7:component/hl7:structuredBody/hl7:component/hl7:section/hl7:entry" +
            "[@typeCode=\"DRIV\"]/hl7:organizer[@classCode=\"CLUSTER\" and @moodCode=\"EVN\"]/..", rrNames);

        // find the status in the RR utilizing the templateid root
        // codes specified from the APHL/LAC Spec
        var rrEntryForStatusCodes = rrXDocument.XPathSelectElements(
            "/*/hl7:component/hl7:structuredBody/hl7:component/hl7:section/hl7:templateId" +
            "[@root=\"2.16.840.1.113883.10.20.15.2.2.3\"]/../hl7:entry/hl7:act/hl7:templateId[@root=\"2.16.840.1.113883.10.20.15.2.3.29\"]/../..",
            rrNames);

        // Create the section element with root-level elements
        // and entry to insert in the eICR
        if (rrEntry is not null)
        {
          var ecrSection = new XElement("section");
          foreach (var element in rrElements)
          {
            ecrSection.Add(element);
          }

          if (rrEntryForStatusCodes is not null)
          {
            ecrSection.Add(rrEntryForStatusCodes);
          }

          ecrSection.Add(rrEntry);

          // Append the ecr section into the eCR - puts it at the end
          ecrXDocument.Root!.Add(ecrSection);
        }

        return ecrXDocument;
      }
      catch (Exception ex)
      {
        throw new UserFacingException($"Error processing eICR document: {ex.Message}", HttpStatusCode.InternalServerError, ex);
      }
    }

    /// <summary>
    ///  Given an HL7 XML string the function will attempt to set text
    ///  for all reference tags based on the value attribute.
    /// </summary>
    /// <param name="ecrXDocument">HL7 XML document.</param>
    /// <returns>XML document with text set for references.</returns>
    private static XDocument ResolveReferences(XDocument ecrXDocument)
    {
      var names = new XmlNamespaceManager(ecrXDocument.CreateNavigator().NameTable);
      names.AddNamespace("hl7", "urn:hl7-org:v3");

      var refs = ecrXDocument.XPathSelectElements(
          "//hl7:reference",
          names);

      foreach (var refElement in refs)
      {
        var value = refElement.Attribute("value");
        if (value is not null)
        {
          var refId = value.Value[1..];
          // The XPath expression evaluated to unexpected type System.Xml.Linq.XText.
          var refText = ecrXDocument.XPathEvaluate(
              $"//*[@ID='{refId}']/text()",
              names);

          if (refText is IEnumerable<object> enumerableSequence)
          {
            IEnumerable<XObject> enumerableText = enumerableSequence.Cast<XObject>();
            refElement.Value = string.Join(" ", enumerableText.Select(t => t.ToString()));
          }
        }
      }

      return ecrXDocument;
    }
  }
}