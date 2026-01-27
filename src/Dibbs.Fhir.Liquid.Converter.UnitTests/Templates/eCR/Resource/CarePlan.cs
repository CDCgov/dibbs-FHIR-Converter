using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Xunit;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests
{
    public class CarePlanTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "Resource",
            "CarePlan.liquid"
        );

        [Fact]
        public void CarePlan_AllFields_Act()
        {
            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "patientReference", "Patient/4566" },
                {
                    "carePlan",
                    Hash.FromAnonymousObject(
                        new {
                            id = new { root = "ab1791b0-5c71-11db-b0de-0800200c9a54", },
                            entry = new {
                                act = new {
                                    id = new { root = "ab1791b0-5c71-11db-b0de-0800200c9a55", },
                                    moodCode = "RQO",
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
                                            typeCode = "RSON",
                                            observation = new { value =  new { code = "Couldn't hurt" } },
                                        },
                                    },
                                },
                            }
                        }
                    )
                },
            };
            var actualFhir = GetFhirObjectFromTemplate<CarePlan>(ECRPath, attributes);

            Assert.Equal("CarePlan", actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);
            Assert.Equal(actualFhir.Status, RequestStatus.Unknown);
            Assert.NotEmpty(actualFhir.Activity);
            Assert.Equal(1, actualFhir.Activity.Count());
            var detail = actualFhir.Activity.First().Detail;
            Assert.Equal(CarePlan.CarePlanActivityStatus.Scheduled, detail.Status);
            Assert.Equal(CarePlan.CarePlanActivityKind.Task, detail.Kind);
            Assert.NotEmpty(detail.Scheduled);
            Assert.Equal("Why not", detail.ReasonCode.First().Coding.First().Code);
            Assert.Equal("Couldn't hurt", detail.ReasonCode.Last().Coding.First().Code);
        }

        [Fact]
        public void CarePlan_AllFields_Encounter()
        {
            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "patientReference", "Patient/4566" },
                {
                    "carePlan",
                    Hash.FromAnonymousObject(
                        new {
                            id = new { root = "ab1791b0-5c71-11db-b0de-0800200c9a54", },
                            entry = new {
                                encounter = new {
                                    id = new { root = "ab1791b0-5c71-11db-b0de-0800200c9a55", },
                                    moodCode = "ARQ",
                                    code = new
                                    {
                                        originalText = new
                                        {
                                            _ = "Colonoscopy",
                                        },
                                    },
                                    effectiveTime = new {
                                        low = new { value  = "20201101" },
                                        high = new { value = "20201102"}
                                    },
                                    entryRelationship = new {
                                        typeCode = "RSON",
                                        observation = new { value = new { code = "Why not" } },
                                    },
                                },
                            }
                        }
                    )
                },
            };
            var actualFhir = GetFhirObjectFromTemplate<CarePlan>(ECRPath, attributes);

            Assert.Equal("CarePlan", actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);
            Assert.Equal(actualFhir.Status, RequestStatus.Unknown);
            Assert.Empty(actualFhir.Activity);
        }
        
        [Fact]
        public void CarePlan_AllFields_MultipleEntries()
        {
            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "patientReference", "Patient/4566" },
                {
                    "carePlan",
                    Hash.FromAnonymousObject(
                        new {
                            id = new { root = "ab1791b0-5c71-11db-b0de-0800200c9a54", },
                            entry = new object[] {
                                new {
                                    act = new {
                                        id = new { root = "ab1791b0-5c71-11db-b0de-0800200c9a55", },
                                        moodCode = "RQO",
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
                                                typeCode = "RSON",
                                                observation = new { value =  new { code = "Couldn't hurt" } },
                                            },
                                        },
                                    }
                                },
                                new {
                                    act = new {
                                        id = new { root = "ab1791b0-5c71-11db-b0de-0800200c9a56", },
                                        moodCode = "RQO",
                                        code = new
                                        {
                                            originalText = new
                                            {
                                                _ = "Esophagogastroduodenoscopy",
                                            },
                                        },
                                        effectiveTime = new {
                                            value = "20201101"
                                        },
                                        entryRelationship = new object[] {
                                            new {
                                                typeCode = "RSON",
                                                observation = new { value = new { code = "Another one" } },
                                            },
                                        },
                                    }
                                },
                            }
                        }
                    )
                },
            };
            var actualFhir = GetFhirObjectFromTemplate<CarePlan>(ECRPath, attributes);

            Assert.Equal(2, actualFhir.Activity.Count());
        }
    }
}
