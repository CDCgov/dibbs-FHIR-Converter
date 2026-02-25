using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hl7.Fhir.Model;
using Xunit;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests
{
    public class ProcedureTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "Resource",
            "Procedure.liquid"
        );

        [Fact]
        public void Procedure_AllFields()
        {
            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "fullPatientId", "urn:uuid:9876" },
                {
                    "procedureEntry",
                    new
                        {
                            id = new { root = "ab1791b0-5c71-11db-b0de-0800200c9a54", },
                            statusCode = new { code = "cancelled", },
                            code = new
                            {
                                originalText = new
                                {
                                    _ = "Colonoscopy",
                                },
                            },
                            effectiveTime = new {
                                value = "20201101"
                            },
                            targetSiteCode = new {
                                code = "COLON"
                            },
                            entryRelationship = new object[] {
                                new {
                                    typeCode = "RSON",
                                    observation = new {
                                        value = new { code = "Why not" },
                                        text = new { _ = "Why not" }
                                    }
                                },
                                new {
                                    typeCode = "RSON",
                                    observation = new {
                                        value = new { code = "Couldn't hurt" },
                                        text = new { _ = "Couldn't hurt" }
                                    }
                                },
                                new {
                                    typeCode = "COMP",
                                    observation = new { 
                                        templateId = new { root = "2.16.840.1.113883.10.20.22.4.9" },
                                        value = new { code = "Nausea"}
                                    },
                                },
                                new {
                                    typeCode = "COMP",
                                    observation = new { 
                                        templateId = new { root = "2.16.840.1.113883.10.20.22.4.9" },
                                        value = new { code = "Heartburn"}
                                    },
                                },
                                new {
                                    typeCode = "COMP",
                                    observation = new { 
                                        templateId = new { root = "2.16.840.1.113883.10.20.22.4.999" },
                                        value = new { code = "Red herring"}
                                    },
                                },
                            },
                            methodCode = new { code = "METHOD" },
                            priorityCode = new { code = "CR" },
                            specimen = new [] {
                                new {
                                    specimenRole = new {
                                        specimenPlayingEntity = new { code = new { code = "Tissue" }}
                                    }
                                },
                                new {
                                    specimenRole = new {
                                        specimenPlayingEntity = new { code = new { code = "Bile" }}
                                    }
                                },
                            },
                        }
                },
            };
            var actualFhir = GetFhirObjectFromTemplate<Procedure>(ECRPath, attributes);

            Assert.Equal("Procedure", actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);
            Assert.Equal(
                "http://hl7.org/fhir/us/core/StructureDefinition/us-core-procedure",
                actualFhir.Meta.Profile.First()
            );
            Assert.NotEmpty(actualFhir.Identifier);
            Assert.Equal(EventStatus.NotDone, actualFhir.Status);
            Assert.NotNull(actualFhir.Code);
            Assert.NotNull(actualFhir.Performed);
            Assert.NotEmpty(actualFhir.BodySite);
            Assert.Equal("Why not", actualFhir.ReasonCode.First().Text);
            Assert.Equal("Couldn't hurt", actualFhir.ReasonCode.Last().Text);
            Assert.Equal(
                "METHOD", 
                actualFhir.GetExtensionValue<CodeableConcept>("http://hl7.org/fhir/StructureDefinition/procedure-method").Coding.First().Code
            );
            Assert.Equal(
                "CR", 
                actualFhir.GetExtensionValue<CodeableConcept>("priorityCode").Coding.First().Code
            );
            var specimen = actualFhir.GetExtensions("specimen");
            Assert.Equal("Tissue", ((CodeableConcept)specimen.First().Value).Coding.First().Code);
            Assert.Equal("Bile", ((CodeableConcept)specimen.Last().Value).Coding.First().Code);
        }
    }
}
