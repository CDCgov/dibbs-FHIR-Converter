using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hl7.Fhir.ElementModel.Types;
using Hl7.Fhir.Model;
using Dibbs.Fhir.Liquid.Converter.DataParsers;
using Namotion.Reflection;
using Xunit;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests
{
    public class ProcedurePregnancyFinalRouteMethodDeliveryTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "Resource",
            "ProcedurePregnancyFinalRouteMethodDelivery.liquid"
        );

        [Fact]
        public void FinalRouteMethodDelivery_AllFields()
        {
            var xmlString =
                @"
                <procedure classCode=""PROC"" moodCode=""EVN"">
                    <!-- [C-CDA R2.0] Procedure Activity Procedure -->
                    <templateId root=""2.16.840.1.113883.10.20.22.4.14"" extension=""2014-06-09"" />
                    <!-- [C-CDA PREG] Method of Delivery -->
                    <templateId root=""2.16.840.1.113883.10.20.22.4.299"" extension=""2018-04-01"" />
                    <id root=""bb2d7888-c4c5-440e-903c-b2c0e9551e0f"" />
                    <code code=""237311001""
                        codeSystem=""2.16.840.1.113883.6.96""
                        codeSystemName=""SNOMED CT""
                        displayName=""Breech delivery (procedure)""> </code>
                    <statusCode code=""completed"" />
                    <effectiveTime value=""20180105"" />
                </procedure>";

            var parser = new CcdaDataParser();
            var parsedXml = parser.Parse(xmlString) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "procedureEntry", parsedXml["procedure"] },
            };

            var actualFhir = GetFhirObjectFromTemplate<Procedure>(ECRPath, attributes);

            Assert.Equal("Procedure", actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);
            Assert.Equal(
                "http://hl7.org/fhir/us/bfdr/StructureDefinition/Procedure-final-route-method-delivery",
                actualFhir.Meta.Profile.First()
            );
            Assert.NotEmpty(actualFhir.Identifier);
            Assert.NotEmpty(actualFhir.Category);

            Assert.Equal("Completed", actualFhir.Status.ToString());
            Assert.Equal("http://snomed.info/sct", actualFhir.Code.Coding.First().System);
            Assert.Equal("237311001", actualFhir.Code.Coding.First().Code);
            Assert.Equal("Complete breech delivery", actualFhir.Code.Coding.First().Display);

            Assert.Equal("2018-01-05", (actualFhir.Performed as FhirDateTime)?.Value);
        }
    }
}
