// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Dibbs.Fhir.Liquid.Converter.DataParsers;
using Dibbs.Fhir.Liquid.Converter.Processors;
using Dibbs.Fhir.Liquid.Converter.UnitTests.Mocks;
using Dibbs.Fhir.Liquid.Converter.Utilities;
using Fluid;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests.Processors
{
    public class CcdaDocumentConversionTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DocumentConversion_MatchesStringConversion(bool withoutId)
        {
            var document = XDocument.Load(Path.Join(TestConstants.SampleDataDirectory, "eCR", "eCR_EveEverywoman.xml"));
            if (withoutId)
            {
                document.Root.Elements(document.Root.Name.Namespace + "id").Remove();
            }

            document.Root.Add(new XElement(document.Root.Name.Namespace + "text", "café 😀\r\nsecond line\t& < >"));
            var xml = document.ToString(SaveOptions.DisableFormatting);
            var processor = CreateProcessor();
            var templates = new TemplateProvider(TestConstants.ECRTemplateDirectory);
            using var files = new PhysicalFileProvider(Path.GetFullPath(TestConstants.ECRTemplateDirectory));

            var expected = processor.Convert(xml, "EICR", TestConstants.ECRTemplateDirectory, templates, files);
            var actual = processor.Convert(document, "EICR", TestConstants.ECRTemplateDirectory, templates, files);

            Assert.Equal(expected, actual);
            if (withoutId)
            {
                var bytes = Encoding.UTF8.GetBytes(xml.Replace("\r", string.Empty).Replace("\n", string.Empty));
                var expectedId = new Guid(SHA256.HashData(bytes).AsSpan(0, 16)).ToString();
                Assert.Equal(expectedId, JObject.Parse(actual)["id"]?.Value<string>());
            }
        }

        [Theory]
        [InlineData("<ClinicalDocument xmlns='urn:hl7-org:v3'><text>before <b>bold</b> after</text><value>first&#13;second&#10;third</value></ClinicalDocument>")]
        [InlineData("<ClinicalDocument xmlns='urn:hl7-org:v3'><patient xmlns:sdtc='urn:hl7-org:sdtc'><sdtc:raceCode code='x'/><name>Zoë 😀</name></patient></ClinicalDocument>")]
        [InlineData("<ClinicalDocument><value>2026-01-02</value><value>123</value><value/><text><![CDATA[a < b & c]]></text></ClinicalDocument>")]
        public void DocumentParser_PreservesTemplateData(string xml)
        {
            var document = XDocument.Parse(xml);
            var parser = new CcdaDataParser();
            var expected = (IDictionary<string, object>)parser.Parse(document.ToString(SaveOptions.DisableFormatting));
            expected.Remove("_originalData");

            var actual = parser.Parse(document);

            Assert.Equal(JsonConvert.SerializeObject(expected), JsonConvert.SerializeObject(actual));
        }

        [Fact]
        public void DocumentConversion_PreservesReferencesBeforeNamespaceNormalization()
        {
            var document = XDocument.Parse("""
                <ClinicalDocument xmlns="urn:hl7-org:v3" xmlns:r="urn:example:references">
                  <id root="document"/>
                  <component><section><id root="section"/>
                    <entry><organizer><id root="source"/>
                      <r:entryRelationship typeCode=" COMP " inversionInd=" TRUE ">
                        <r:act><r:templateId root="2.16.840.1.113883.10.20.22.4.122"/>
                          <r:id root="urn:oid:target"/>
                        </r:act>
                      </r:entryRelationship>
                    </organizer></entry>
                  </section></component>
                </ClinicalDocument>
                """);
            const string template = """
                {"resourceType":"Bundle","entry":[
                  {"resource":{"resourceType":"DiagnosticReport","id":"report","identifier":[{"value":"source"}]}},
                  {"resource":{"resourceType":"Procedure","id":"procedure","identifier":[{"value":"target"}]}}
                ]}
                """;
            var templates = Templates("EICR", template);
            var processor = CreateProcessor();
            var files = new MockFileProvider();
            var expected = processor.Convert(document.ToString(SaveOptions.DisableFormatting), "EICR", "", templates, files);

            var actual = processor.Convert(document, "EICR", "", templates, files);

            Assert.Equal(expected, actual);
            var parts = JObject.Parse(actual)["entry"][0]["resource"]["extension"][0]["extension"];
            Assert.Equal("Procedure/procedure", parts[0]["valueReference"]["reference"]?.Value<string>());
            Assert.Equal("COMP", parts[1]["valueCoding"]["code"]?.Value<string>());
            Assert.True(parts[2]["valueBoolean"]?.Value<bool>());
        }

        [Fact]
        public void OtherRootTemplates_PreserveOriginalXmlAttachments()
        {
            var document = XDocument.Parse("<ClinicalDocument><text>café &amp; tea</text></ClinicalDocument>");
            var templates = Templates("Attachment", """{"data":"{{ msg._originalData | gzip }}"}""");
            var processor = CreateProcessor();
            var files = new MockFileProvider();
            var expected = processor.Convert(document.ToString(SaveOptions.DisableFormatting), "Attachment", "", templates, files);

            Assert.Equal(expected, processor.Convert(document, "Attachment", "", templates, files));
        }

        private static CcdaProcessor CreateProcessor() => new CcdaProcessor(NullLogger<CcdaProcessor>.Instance, TemplateUtility.TemplateOptions);

        private static TemplateProvider Templates(string name, string template) => new TemplateProvider(
            new List<Dictionary<string, IFluidTemplate>>
            {
                new Dictionary<string, IFluidTemplate> { [name] = TemplateUtility.Parser.Parse(template) },
            });
    }
}
