using System;
using System.IO;
using Microsoft.Health.Fhir.Liquid.Converter.Tool;
using Xunit;

namespace Microsoft.Health.Fhir.Liquid.Converter.Tool.UnitTests;

public class ConverterLogicHandlerTest
{
    [Fact]
    public void ConvertWithoutSaving_ShouldThrowDirectoryNotFoundException_WhenTemplateDirectoryDoesNotExist()
    {
        var templateDirectory = "nonexistent_directory";
        var rootTemplate = "rootTemplate";
        var inputDataContent = "inputDataContent";
        var isVerboseEnabled = false;
        var isTraceInfo = false;

        var exception = Assert.Throws<DirectoryNotFoundException>(() =>
            ConverterLogicHandler.ConvertWithoutSaving(templateDirectory, rootTemplate, inputDataContent, isVerboseEnabled, isTraceInfo));

        Assert.Equal($"Could not find template directory: {templateDirectory}", exception.Message);
    }

    [Fact]
    public void ConvertWithoutSaving_ShouldThrowFileNotFoundException_WhenMetadataFileDoesNotExist()
    {
        var templateDirectory = "test_directory";
        Directory.CreateDirectory(templateDirectory);
        var rootTemplate = "rootTemplate";
        var inputDataContent = "inputDataContent";
        var isVerboseEnabled = false;
        var isTraceInfo = false;

        try
        {
            var exception = Assert.Throws<FileNotFoundException>(() =>
                ConverterLogicHandler.ConvertWithoutSaving(templateDirectory, rootTemplate, inputDataContent, isVerboseEnabled, isTraceInfo));

            Assert.Equal($"Could not find metadata.json in template directory: {templateDirectory}.", exception.Message);
        }
        finally
        {
            Directory.Delete(templateDirectory);
        }
    }

    [Fact]
    public void ConvertWithoutSaving_ShouldThrowNotImplementedException_WhenDataTypeIsNotSupported()
    {
        var templateDirectory = "test_directory";
        Directory.CreateDirectory(templateDirectory);
        var metadataFilePath = Path.Combine(templateDirectory, "metadata.json");
        File.WriteAllText(metadataFilePath, "{\"Type\": \"unsupportedType\"}");
        var rootTemplate = "rootTemplate";
        var inputDataContent = "inputDataContent";
        var isVerboseEnabled = false;
        var isTraceInfo = false;

        try
        {
            var exception = Assert.Throws<NotImplementedException>(() =>
                ConverterLogicHandler.ConvertWithoutSaving(templateDirectory, rootTemplate, inputDataContent, isVerboseEnabled, isTraceInfo));

            Assert.Equal("The conversion from data type 'unsupportedType' to FHIR is not supported", exception.Message);
        }
        finally
        {
            File.Delete(metadataFilePath);
            Directory.Delete(templateDirectory);
        }
    }

    [Fact]
    public void ConvertWithoutSaving_ShouldReturnConvertedContent_WhenValidInputIsProvided()
    {
        var templateDirectory = "test_directory";
        Directory.CreateDirectory(templateDirectory);
        var metadataFilePath = Path.Combine(templateDirectory, "metadata.json");
        File.WriteAllText(metadataFilePath, "{\"Type\": \"ccda\"}");
        var eICRFilePath = Path.Combine(templateDirectory, "eICR.liquid");
        File.WriteAllText(eICRFilePath, "{\"resourceType\": \"Bundle\"}");
        var rootTemplate = "eICR";
        var inputDataContent = "<ClinicalDocument xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"></ClinicalDocument>";
        var isVerboseEnabled = false;
        var isTraceInfo = false;

        try
        {
            var result = ConverterLogicHandler.ConvertWithoutSaving(templateDirectory, rootTemplate, inputDataContent, isVerboseEnabled, isTraceInfo);

            Assert.NotNull(result);
        }
        finally
        {
            File.Delete(metadataFilePath);
            File.Delete(eICRFilePath);
            Directory.Delete(templateDirectory);
        }
    }
}