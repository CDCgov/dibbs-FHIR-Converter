using System.Net;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Dibbs.FhirConverterApi.Models;

namespace Dibbs.FhirConverterApi.Processors;

public class EcrProcessor
{
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

            // Check for eICR Processing Status entry (required & only available in RR)
            if (ecrXDocument.XPathSelectElement("//*[@root=\"2.16.840.1.113883.10.20.15.2.3.29\"]") is not null)
            {
                Console.WriteLine("This eCR has already been merged with RR data.");
                return ecrXDocument;
            }

            // Create the tags for elements we'll be looking for
            string[] rrTags =
            [
                "templateId",
                "id",
                "code",
                "title",
                "effectiveTime",
                "confidentialityCode",
            ];

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
                // Makes sure xmlns:xsi is set since in case RR references it and eICR doesn't
                XNamespace xsi = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance");
                ecrXDocument.Root!.SetAttributeValue(XNamespace.Xmlns + "xsi", xsi);
                XNamespace ns = "urn:hl7-org:v3";
                var ecrSection = new XElement(ns + "section");
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
    public static XDocument ResolveReferences(XDocument ecrXDocument)
    {
        var names = new XmlNamespaceManager(ecrXDocument.CreateNavigator().NameTable);
        names.AddNamespace("hl7", "urn:hl7-org:v3");

        // First pass: build a lookup of all elements with an ID attribute
        // This replaces the per-reference XPath query with a single dictionary lookup
        var idLookup = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var element in ecrXDocument.Descendants())
        {
            var id = element.Attribute("ID")?.Value;
            if (id != null && !idLookup.ContainsKey(id))
            {
                var elementValue = string.Concat(element.Nodes().Select(n => n.ToString()));
                idLookup[id] = elementValue;
            }
        }

        // Second pass: resolve all references using the lookup
        var refs = ecrXDocument.XPathSelectElements("//hl7:reference", names);
        foreach (var refElement in refs)
        {
            var value = refElement.Attribute("value");
            if (value is not null)
            {
                var refId = value.Value[1..];
                if (idLookup.TryGetValue(refId, out var elementValue))
                {
                    refElement.Value = elementValue;
                }
            }
        }

        return ecrXDocument;
    }

    public static XDocument ResolveEntryReferences(XDocument ecrXDocument)
    {
        var element = ecrXDocument.Root;

        if (element != null)
        {
            var clinicalStatementById = GetClinicalStatementIdLookup(element);
            element = ResolveEntryReferenceActs(element, clinicalStatementById);
            ecrXDocument.Root?.ReplaceWith(element);
        }

        return ecrXDocument;
    }

    private static Dictionary<string, XElement> GetClinicalStatementIdLookup(XElement element)
    {
        var clinicalStatementById = new Dictionary<string, XElement>(StringComparer.Ordinal);

        foreach (var statement in element.DescendantsAndSelf().Where(IsClinicalStatementElement))
        {
            // Skip Entry References because they reuse the target's id.
            if (IsEntryReferenceAct(statement))
            {
                continue;
            }

            // Store a copy so later replacements do not change the statement used to resolve other references.
            var statementCopy = new XElement(statement);
            foreach (var idElement in GetIdElements(statement))
            {
                var key = GetIdentifierKey(idElement);
                if (key != null && !clinicalStatementById.ContainsKey(key))
                {
                    clinicalStatementById[key] = statementCopy;
                }
            }
        }

        return clinicalStatementById;
    }

    private static XElement ResolveEntryReferenceActs(
        XElement element,
        Dictionary<string, XElement> clinicalStatementById)
    {
        foreach (var entryReferenceAct in element.DescendantsAndSelf().Where(IsEntryReferenceAct).ToList())
        {
            var replacement = GetEntryReferenceReplacement(entryReferenceAct, clinicalStatementById);

            if (replacement != null)
            {
                entryReferenceAct.ReplaceWith(replacement);
            }
        }

        return element;
    }

    private static XElement? GetEntryReferenceReplacement(
        XElement entryReferenceAct,
        Dictionary<string, XElement> clinicalStatementById)
    {
        foreach (var idElement in GetIdElements(entryReferenceAct))
        {
            var key = GetIdentifierKey(idElement);

            // Stop if resolving this reference would create a cycle.
            if (key == null ||
                !clinicalStatementById.TryGetValue(key, out var referencedStatement))
            {
                continue;
            }

            var replacement = new XElement(referencedStatement);
            ResolveEntryReferenceActs(replacement, clinicalStatementById);
            return replacement;
        }

        return null;
    }

    private static IEnumerable<XElement> GetIdElements(XElement element)
    {
        return element.Elements().Where(child => child.Name.LocalName == "id");
    }

    private static string? GetIdentifierKey(XElement idElement)
    {
        if (idElement.Attribute("nullFlavor") != null)
        {
            return null;
        }

        var root = idElement.Attribute("root")?.Value;
        var extension = idElement.Attribute("extension")?.Value;

        if (string.IsNullOrWhiteSpace(root) && string.IsNullOrWhiteSpace(extension))
        {
            return null;
        }

        return $"{root}\u001f{extension}";
    }

    private static bool IsClinicalStatementElement(XElement element)
    {
        // These are the Clinical Statements that can be contained by Entry or EntryRelationship
        // https://build.fhir.org/ig/HL7/CDA-core-sd/StructureDefinition-Entry.html
        // https://build.fhir.org/ig/HL7/CDA-core-sd/StructureDefinition-EntryRelationship.html
        HashSet<string> clinicalStatementElementNames =
        [
            "act",
            "encounter",
            "observation",
            "observationMedia",
            "organizer",
            "procedure",
            "regionOfInterest",
            "substanceAdministration",
            "supply",
        ];

        return clinicalStatementElementNames.Contains(element.Name.LocalName);
    }

    private static bool IsEntryReferenceAct(XElement element)
    {
        return element.Name.LocalName == "act" &&
               element.Elements()
                   .Any(child => child.Name.LocalName == "templateId" &&
                                 child.Attribute("root")?.Value == "2.16.840.1.113883.10.20.22.4.122");
    }
}
