using System;
using System.IO;
using Microsoft.Health.Fhir.Liquid.Converter.Tool;
using Microsoft.Health.Fhir.Liquid.Converter.Tool.Models;
using Xunit;

namespace Microsoft.Health.Fhir.Liquid.Converter.Tool.UnitTests;

public class ConverterLogicHandlerTest
{
    [Fact]
    public void ConvertWithoutSaving_ShouldThrowDirectoryNotFoundException_WhenTemplateDirectoryDoesNotExist()
    {
        var options = new ConverterOptions()
        {
            TemplateDirectory = "nonexistent_directory",
            RootTemplate = "rootTemplate",
            InputDataContent = "inputDataContent",
        };

        var exception = Assert.Throws<DirectoryNotFoundException>(() =>
            ConverterLogicHandler.ConvertWithoutSaving(options));

        Assert.Equal($"Could not find template directory: {options.TemplateDirectory}", exception.Message);
    }

    [Fact]
    public void ConvertWithoutSaving_ShouldThrowFileNotFoundException_WhenMetadataFileDoesNotExist()
    {
        var options = new ConverterOptions()
        {
            TemplateDirectory = "test_directory",
            RootTemplate = "rootTemplate",
            InputDataContent = "inputDataContent",
        };

        Directory.CreateDirectory(options.TemplateDirectory);

        try
        {
            var exception = Assert.Throws<FileNotFoundException>(() =>
                ConverterLogicHandler.ConvertWithoutSaving(options));

            Assert.Equal($"Could not find metadata.json in template directory: {options.TemplateDirectory}.", exception.Message);
        }
        finally
        {
            Directory.Delete(options.TemplateDirectory);
        }
    }

    [Fact]
    public void ConvertWithoutSaving_ShouldThrowNotImplementedException_WhenDataTypeIsNotSupported()
    {
        var options = new ConverterOptions()
        {
            TemplateDirectory = "test_directory",
            RootTemplate = "rootTemplate",
            InputDataContent = "inputDataContent",
        };

        Directory.CreateDirectory(options.TemplateDirectory);
        var metadataFilePath = Path.Combine(options.TemplateDirectory, "metadata.json");
        File.WriteAllText(metadataFilePath, "{\"Type\": \"unsupportedType\"}");

        try
        {
            var exception = Assert.Throws<NotImplementedException>(() =>
                ConverterLogicHandler.ConvertWithoutSaving(options));

            Assert.Equal("The conversion from data type 'unsupportedType' to FHIR is not supported", exception.Message);
        }
        finally
        {
            File.Delete(metadataFilePath);
            Directory.Delete(options.TemplateDirectory);
        }
    }

    [Fact]
    public void ConvertWithoutSaving_ShouldReturnConvertedContent_WhenValidInputIsProvided()
    {
        var options = new ConverterOptions()
        {
            TemplateDirectory = "test_directory",
            RootTemplate = "eICR",
            InputDataContent = "<ClinicalDocument xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"></ClinicalDocument>",
        };

        Directory.CreateDirectory(options.TemplateDirectory);
        var metadataFilePath = Path.Combine(options.TemplateDirectory, "metadata.json");
        File.WriteAllText(metadataFilePath, "{\"Type\": \"ccda\"}");
        var eICRFilePath = Path.Combine(options.TemplateDirectory, "eICR.liquid");
        File.WriteAllText(eICRFilePath, "{\"resourceType\": \"Bundle\"}");


        try
        {
            var result = ConverterLogicHandler.ConvertWithoutSaving(options);

            Assert.NotNull(result);
        }
        finally
        {
            File.Delete(metadataFilePath);
            File.Delete(eICRFilePath);
            Directory.Delete(options.TemplateDirectory);
        }
    }
}