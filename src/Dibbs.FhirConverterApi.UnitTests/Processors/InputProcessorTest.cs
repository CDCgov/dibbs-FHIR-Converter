using System.Net;
using System.Xml;
using Dibbs.FhirConverterApi.Models;
using Dibbs.FhirConverterApi.Processors;

namespace Dibbs.FhirConverterApi.UnitTests.Processors;

public class InputProcessorTest
{
    [Theory]
    [InlineData("<ClinicalDocument xmlns=\"urn:hl7-org:v3\" />")]
    [InlineData("<cda:ClinicalDocument xmlns:cda=\"urn:hl7-org:v3\" />")]
    public void DetermineDocumentType_ReturnsCcda_WhenRootIsClinicalDocument(string input)
    {
        using var reader = CreateReader(input);

        var actual = InputProcessor.DetermineDocumentType(reader);

        Assert.Equal(InputDocumentType.Ccda, actual);
    }

    [Theory]
    [InlineData("<Bundle xmlns=\"http://hl7.org/fhir\" />")]
    [InlineData("<fhir:Bundle xmlns:fhir=\"http://hl7.org/fhir\" />")]
    public void DetermineDocumentType_ReturnsFhir_WhenRootIsBundle(string input)
    {
        using var reader = CreateReader(input);

        var actual = InputProcessor.DetermineDocumentType(reader);

        Assert.Equal(InputDocumentType.Fhir, actual);
    }

    [Theory]
    [InlineData("<ClinicalDocument />")]
    [InlineData("<Bundle />")]
    [InlineData("<Patient xmlns=\"http://hl7.org/fhir\" />")]
    public void DetermineDocumentType_Throws_WhenRootIsUnsupported(string input)
    {
        using var reader = CreateReader(input);

        var exception = Assert.Throws<UserFacingException>(() => InputProcessor.DetermineDocumentType(reader));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, exception.StatusCode);
        Assert.Equal(
            "Unsupported XML root element. Expected a C-CDA ClinicalDocument or FHIR R4 Bundle.",
            exception.Message);
    }

    private static XmlReader CreateReader(string input)
    {
        return XmlReader.Create(
            new StringReader(input),
            new XmlReaderSettings
            {
                CloseInput = true,
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
            });
    }
}
