using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Dibbs.Fhir.Liquid.Converter.Utilities;
using Microsoft.Extensions.FileProviders;
using Xunit;
using Fluid;
using Dibbs.Fhir.Liquid.Converter.Models;
using Fluid.Values;
using System.Threading.Tasks;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests
{
    public class BaseECRLiquidTests
    {
        private static JsonDocumentOptions jsonDeserializeOptions =
            new() { AllowTrailingCommas = true, };

        private static TemplateOptions templateOptions;
        private static ITemplateProvider templateProvider;
        private static CodeMapping codeMapping;

        static BaseECRLiquidTests()
        {
            var options = new TemplateOptions();
            var fileProvider = new PhysicalFileProvider(Path.GetFullPath(TestConstants.ECRTemplateDirectory));
            options.FileProvider = fileProvider;

            // This is necessary so that we can access child objects from context
            options.MemberAccessStrategy = new UnsafeMemberAccessStrategy();
            TemplateUtility.AddFilters(options);

            templateOptions = options;

            // Set up the context
            templateProvider = new TemplateProvider(
                TestConstants.ECRTemplateDirectory
            );

            // Add the value sets to the context
            var codeContent = File.ReadAllText(
                Path.Join(TestConstants.ECRTemplateDirectory, "ValueSet", "ValueSet.json")
            );

            codeMapping = JsonSerializer.Deserialize<CodeMapping>(codeContent);
        }

        /// <summary>
        /// Given a path to an eCR template, and attributes, render the template.
        /// </summary>
        /// <param name="templatePath">Path to the template being tested</param>
        /// <param name="attributes">Dictionary of attributes to hydrate the template</param>
        protected static async Task<string> RenderLiquidTemplate(
            string templatePath,
            Dictionary<string, object> attributes
        )
        {
            var templateContent = File.ReadAllText(templatePath);
            var template = TemplateUtility.ParseLiquidTemplate(templatePath, templateContent);
            Assert.True(template != null);

            var context = new TemplateContext(templateOptions);
            context.SetValue("file_system", templateProvider.GetTemplateFileSystem());

            if (codeMapping != null)
            {
                context.SetValue("CodeMapping", codeMapping);
            }

            // Hydrate the context with the attributes passed to the function
            foreach (var keyValue in attributes)
            {
                context.SetValue(keyValue.Key, keyValue.Value);
            }

            // Render and strip out unhelpful whitespace (actual post-processing gets rid of this
            // at the end of the day anyway)
            var actualContent = "";
            try
            {
                actualContent = template.Render(context)
                .Trim()
                .Replace("\n", " ")
                .Replace("\t", string.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return (await Filters.CleanStringFromTabs(StringValue.Create(actualContent), FilterArguments.Empty, context)).ToStringValue();
        }

        /// <summary>
        /// Checks that the rendered template matches the expected contents.
        /// </summary>
        /// <param name="templatePath">Path to the template being tested</param>
        /// <param name="attributes">Dictionary of attributes to hydrate the template</param>
        /// <param name="expectedContent">Serialized string that should be returned</param>
        protected static async System.Threading.Tasks.Task ConvertCheckLiquidTemplate(
            string templatePath,
            Dictionary<string, object> attributes,
            string expectedContent
        )
        {
            var actualContent = await RenderLiquidTemplate(templatePath, attributes);
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
            const int timeoutMs = 10000;

            var renderTask = System.Threading.Tasks.Task.Run(async () => await RenderLiquidTemplate(templatePath, attributes));

            if (System.Threading.Tasks.Task.WhenAny(renderTask, System.Threading.Tasks.Task.Delay(timeoutMs)).Result != renderTask)
            {
                Console.WriteLine($"Liquid template rendering timed out after {timeoutMs}ms. Template: {templatePath}");
                throw new TimeoutException();
            }

            var actual = renderTask.Result;
            var actualJson = DeserializeJson(actual);

            // If the JSON is a FHIR resource, then it will have a `resource` property, and we just
            // want the value of `resource. Otherwise, if it does not have a `resource` property, it
            // is not a resource we do not need to change the JSON.
            if (actualJson.TryGetProperty("resource", out var resourceElement))
            {
                actualJson = resourceElement;
            }

            var fhirOptions = new JsonSerializerOptions { AllowTrailingCommas = true, }
                .ForFhir(ModelInfo.ModelInspector)
                .UsingMode(DeserializerModes.Ostrich);

            var actualFhir = JsonSerializer.Deserialize<T>(actualJson, fhirOptions);

            return actualFhir;
        }

        /// <summary>
        /// Create a FHIR object from the output of a template that does not return fully formed JSON.
        /// </summary>
        /// <typeparam name="T">FHIR Type</typeparam>
        /// <param name="templatePath">Path to the template being tested</param>
        /// <param name="attributes">Dictionary of attributes to hydrate the template</param>
        /// <returns>FHIR object of type T</returns>
        protected async static Task<T> GetFhirObjectFromPartialTemplate<T>(
            string templatePath,
            Dictionary<string, object> attributes
        )
        {
            // Wraps the rendered template in curly braces to make it a valid JSON object.
            var actual = $"{{ { await RenderLiquidTemplate(templatePath, attributes) } }}";
            var actualJson = DeserializeJson(actual);
            var fhirOptions = new JsonSerializerOptions { AllowTrailingCommas = true, }
                .ForFhir(ModelInfo.ModelInspector)
                .UsingMode(DeserializerModes.Ostrich);
            var actualFhir = JsonSerializer.Deserialize<T>(actualJson, fhirOptions);

            return actualFhir;
        }

        protected static async System.Threading.Tasks.Task CompareJSONOutput(
            string templatePath,
            Dictionary<string, object> attributes,
            string expectedPath
        )
        {
            var content = await RenderLiquidTemplate(templatePath, attributes);
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
