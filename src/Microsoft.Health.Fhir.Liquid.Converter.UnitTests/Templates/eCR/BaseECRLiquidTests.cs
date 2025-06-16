using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using DotLiquid;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Health.Fhir.Liquid.Converter.Utilities;
using Xunit;

namespace Microsoft.Health.Fhir.Liquid.Converter.UnitTests
{
    public class BaseECRLiquidTests
    {
        private static JsonDocumentOptions jsonDeserializeOptions =
            new() { AllowTrailingCommas = true, };

        /// <summary>
        /// Given a path to an eCR template, and attributes, render the template.
        /// </summary>
        /// <param name="templatePath">Path to the template being tested</param>
        /// <param name="attributes">Dictionary of attributes to hydrate the template</param>
        protected static string RenderLiquidTemplate(
            string templatePath,
            Dictionary<string, object> attributes
        )
        {
            var templateContent = File.ReadAllText(templatePath);
            var template = TemplateUtility.ParseLiquidTemplate(templatePath, templateContent);
            Assert.True(template.Root.NodeList.Count > 0);

            // Set up the context
            var templateProvider = new TemplateProvider(
                TestConstants.ECRTemplateDirectory,
                Liquid.Converter.Models.DataType.Ccda
            );
            var context = new Context(
                environments: new List<Hash>(),
                outerScope: new Hash(),
                registers: Hash.FromDictionary(
                    new Dictionary<string, object>()
                    {
                        { "file_system", templateProvider.GetTemplateFileSystem() },
                    }
                ),
                errorsOutputMode: ErrorsOutputMode.Display,
                maxIterations: 0,
                formatProvider: CultureInfo.InvariantCulture,
                cancellationToken: CancellationToken.None
            );
            context.AddFilters(typeof(Filters));

            // Add the value sets to the context
            var codeContent = File.ReadAllText(
                Path.Join(TestConstants.ECRTemplateDirectory, "ValueSet", "ValueSet.json")
            );
            var codeMapping = TemplateUtility.ParseCodeMapping(codeContent);
            Console.WriteLine(codeMapping);
            if (codeMapping?.Root?.NodeList?.First() != null)
            {
                context["CodeMapping"] = codeMapping.Root.NodeList.First();
            }

            // Hydrate the context with the attributes passed to the function
            foreach (var keyValue in attributes)
            {
                context[keyValue.Key] = keyValue.Value;
            }

            // Render and strip out unhelpful whitespace (actual post-processing gets rid of this
            // at the end of the day anyway)
            var actualContent = template
                .Render(RenderParameters.FromContext(context, CultureInfo.InvariantCulture))
                .Trim()
                .Replace("\n", " ")
                .Replace("\t", string.Empty);

            // Many are harmless, but can be helpful for debugging
            foreach (var err in template.Errors)
            {
                Console.WriteLine(err.Message);
            }

            return Filters.CleanStringFromTabs(actualContent);
        }

        /// <summary>
        /// Checks that the rendered template matches the expected contents.
        /// </summary>
        /// <param name="templatePath">Path to the template being tested</param>
        /// <param name="attributes">Dictionary of attributes to hydrate the template</param>
        /// <param name="expectedContent">Serialized string that should be returned</param>
        protected static void ConvertCheckLiquidTemplate(
            string templatePath,
            Dictionary<string, object> attributes,
            string expectedContent
        )
        {
            var actualContent = RenderLiquidTemplate(templatePath, attributes);
            Assert.Equal(expectedContent, actualContent);
        }

        /// <summary>
        /// Create a FHIR object from the output of a template.
        /// </summary>
        /// <typeparam name="T">FHIR Type</typeparam>
        /// <param name="templatePath">Path to the template being tested</param>
        /// <param name="attributes">Dictionary of attributes to hydrate the template</param>
        /// <returns>FHIR object of type T</returns>
        protected static T GetFhirObjectFromTemplate<T>(
            string templatePath,
            Dictionary<string, object> attributes
        )
        {
            var actual = RenderLiquidTemplate(templatePath, attributes);
            var actualJson = DeserializeJson(actual);
            var actualResource = actualJson.GetProperty("resource");

            var fhirOptions = new JsonSerializerOptions { AllowTrailingCommas = true, }
                .ForFhir(ModelInfo.ModelInspector)
                .UsingMode(DeserializerModes.Ostrich);
            var actualFhir = JsonSerializer.Deserialize<T>(actualResource, fhirOptions);

            return actualFhir;
        }

        protected static void CompareJSONOutput(
            string templatePath,
            Dictionary<string, object> attributes,
            string expectedPath
        )
        {
            var content = RenderLiquidTemplate(templatePath, attributes);
            var actualMinimized = MinimizeJson(content);

            var expected = File.ReadAllText(
                Path.Join(TestConstants.ExpectedDirectory, expectedPath)
            );

            var expectedMinimized = MinimizeJson(expected);

            Assert.Equal(expectedMinimized, actualMinimized);
        }

        protected static JsonElement DeserializeJson(string content)
        {
            return JsonDocument.Parse(content.TrimEnd(new [] {',', ' '}), jsonDeserializeOptions).RootElement;
        }

        protected static string MinimizeJson(string content)
        {
            var jsonObject = DeserializeJson(content);
            var minimizedJsonString = JsonSerializer.Serialize(jsonObject);
            return minimizedJsonString;
        }
    }
}
