using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hl7.Fhir.Model;
using Xunit;
using Dibbs.Fhir.Liquid.Converter.DataParsers;
using System;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests
{
    public class DocumentReferenceRRTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "Resource",
            "DocumentReferenceRR.liquid"
        );

        [Fact]
        public void DocumentReferenceRR_AllFields()
        {
            // from 3.1 eCR
            var xmlStr = @"
            <act classCode=""ACT"" moodCode=""EVN""
                xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                xsi:schemaLocation=""urn:hl7-org:v3 ../../../cda-core-2.0/schema/extensions/SDTC/infrastructure/cda/CDA_SDTC.xsd""
                xmlns=""urn:hl7-org:v3""
                xmlns:cda=""urn:hl7-org:v3""
                xmlns:sdtc=""urn:hl7-org:sdtc""
                xmlns:voc=""http://www.lantanagroup.com/voc"">
                <templateId extension=""2017-04-01""
                    root=""2.16.840.1.113883.10.20.15.2.3.20"" />
                <id root=""814d6c77-aa2c-4dfd-8f62-d5a0df4b5bbb"" />
                <code code=""RRVS8""
                    codeSystem=""2.16.840.1.114222.4.5.274""
                    codeSystemName=""PHINs VS (CDC Local Coding System)""
                    displayName=""Additional reporting needs"" />
                <priorityCode code=""RRVS17""
                    codeSystem=""2.16.840.1.114222.4.5.274""
                    codeSystemName=""PHIN VS (CDC Local Coding System)""
                    displayName=""Immediate action required"" />
                <reference typeCode=""REFR"">
                    <externalDocument classCode=""DOC""
                        moodCode=""EVN"">
                        <templateId extension=""2017-04-01""
                            root=""2.16.840.1.113883.10.20.15.2.3.17"" />
                        <code nullFlavor=""OTH"">
                            <originalText>Additional information
                                for the required reporting of
                                Zika must be submitted to State
                                Department of Health
                                immediately. This additional
                                information can be submitted
                                here.</originalText>
                        </code>
                        <text mediaType=""text/html"">
                            <reference
                                value=""http://statedepartmentofhealth.gov/epi/disease/zika/Supplemental_data_form.pdf"" />
                        </text>
                    </externalDocument>
                </reference>
            </act>
            ";

            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;
            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "documentReference", parsed["act"]},
            };
            var actualFhir = GetFhirObjectFromTemplate<DocumentReference>(ECRPath, attributes);

            Assert.Equal(ResourceType.DocumentReference.ToString(), actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);
            Assert.Equal(DocumentReferenceStatus.Current.ToString(), actualFhir.Status.ToString());
            Assert.Equal(
                "http://hl7.org/fhir/us/ecr/StructureDefinition/rr-documentreference",
                actualFhir.Meta.Profile.First()
            );

            Assert.Equal("Public health Note", actualFhir.Type.Coding.First().Display);
            Assert.Equal("83910-0", actualFhir.Type.Coding.First().Code);
            Assert.Equal("Public health information", actualFhir.Type.Text);

            Assert.Equal("RRVS8", actualFhir.Category.First().Coding.First().Code);
            Assert.Equal("Additional reporting needs", actualFhir.Category.First().Coding.First().Display);

            Assert.Equal("Additional information for the required reporting of Zika must be submitted to State Department of Health immediately. This additional information can be submitted here.", actualFhir.Description);
            Assert.Equal("http://statedepartmentofhealth.gov/epi/disease/zika/Supplemental_data_form.pdf", actualFhir.Content.First().Attachment.Url);
            Assert.Equal("text/html", actualFhir.Content.First().Attachment.ContentType);
            
            Assert.Equal("Immediate action required", actualFhir.GetExtensionValue<CodeableConcept>("http://hl7.org/fhir/us/ecr/StructureDefinition/rr-priority-extension").Coding.First().Display);
            Assert.Equal("RRVS17", actualFhir.GetExtensionValue<CodeableConcept>("http://hl7.org/fhir/us/ecr/StructureDefinition/rr-priority-extension").Coding.First().Code);
        }
    }
}