using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotLiquid;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Xunit;

namespace Microsoft.Health.Fhir.Liquid.Converter.UnitTests
{
    public class ServiceRequestTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "Resource",
            "_ServiceRequest.liquid"
        );

        [Fact]
        public void ServiceRequest_AllFields()
        {
            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "patientReference", "Patient/4566" },
                {
                    "serviceEntry",
                    Hash.FromAnonymousObject(
                        new {
                            id = new { root = "ab1791b0-5c71-11db-b0de-0800200c9a54", },
                            moodCode = "RQO",
                            statusCode = new { code = "active" },
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
                            entryRelationship = new object[] {
                                new {
                                    typeCode = "RSON",
                                    observation = new { value = new { code = "Why not" } },
                                },
                                new {
                                    typeCode = "REFR",
                                    observation = new { 
                                        templateId = new { root = "2.16.840.1.113883.10.20.22.4.143" },
                                        value =  new { code = "A" } 
                                    },
                                },
                            },
                            author = new {
                                time = "20201030"
                            },
                            targetSiteCode = new { code = "Abdomen" },
                        }
                    )
                },
            };
            var actualFhir = GetFhirObjectFromTemplate<ServiceRequest>(ECRPath, attributes);
            var str = new FhirJsonPocoSerializer().SerializeToString(actualFhir);
            Console.WriteLine(str);

            Assert.Equal("ServiceRequest", actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);
            Assert.Equal(RequestStatus.Active, actualFhir.Status);
            Assert.NotEmpty(actualFhir.Occurrence);
            Assert.Equal("Why not", actualFhir.ReasonCode.First().Coding.First().Code);
            Assert.Equal(RequestPriority.Asap , actualFhir.Priority);
            Assert.NotEmpty(actualFhir.BodySite);
        }
    }
}
