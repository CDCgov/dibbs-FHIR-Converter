using System.Net;
using System.Xml;
using System.Xml.Linq;
using Dibbs.FhirConverterApi.Models;

namespace Dibbs.FhirConverterApi.Processors;

public static class InputProcessor
{
    private static readonly XName CcdaRoot = XName.Get("ClinicalDocument", "urn:hl7-org:v3");

    private static readonly XName FhirRoot = XName.Get("Bundle", "http://hl7.org/fhir");

    /// <summary>
    /// Determines whether an XML document is a C-CDA document or a FHIR R4 Bundle.
    /// </summary>
    /// <param name="reader">An XML reader at the beginning of the stream or on its root element.</param>
    /// <returns>The detected input document type.</returns>
    /// <exception cref="UserFacingException">Thrown when the XML root is not supported.</exception>
    /// <exception cref="XmlException">Thrown when the reader does not contain an XML root element.</exception>
    public static InputDocumentType DetermineDocumentType(XmlReader reader)
    {
        if (reader.MoveToContent() != XmlNodeType.Element)
        {
            throw new XmlException("XML document does not contain a root element.");
        }

        var rootName = XName.Get(reader.LocalName, reader.NamespaceURI);

        if (rootName == CcdaRoot)
        {
            return InputDocumentType.Ccda;
        }

        if (rootName == FhirRoot)
        {
            return InputDocumentType.Fhir;
        }

        throw new UserFacingException(
            "Unsupported XML root element. Expected a C-CDA ClinicalDocument or FHIR R4 Bundle.",
            HttpStatusCode.UnprocessableEntity);
    }
}
