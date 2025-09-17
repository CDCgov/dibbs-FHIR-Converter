using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using DotLiquid;
using Hl7.Fhir.Model;
using Xunit;
using Xunit.Abstractions;
using Microsoft.Health.Fhir.Liquid.Converter.Parsers;


namespace Microsoft.Health.Fhir.Liquid.Converter.UnitTests
{
    public class AdverseEventTests : BaseECRLiquidTests
    {

        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "Resource",
            "_AdverseEvent.liquid"
        );

        [Fact]
        public void AdverseEvent_AllFields()
        {
                     // from 3.1 spec
                     var xmlStr = @"
                     <observation classCode=""OBS"" moodCode=""EVN"">
                         <templateId root=""2.16.840.1.113883.10.20.22.4.9"" extension=""2014-06-09"" />
                         <id root=""4adc1020-7b14-11db-9fe1-0800200c9a64"" />
                         <code code=""ASSERTION"" codeSystem=""2.16.840.1.113883.5.4"" />
                         <text>
                             <reference value=""#reaction1"" />
                         </text>
                         <statusCode code=""completed"" />
                         <effectiveTime>
                             <low value=""200802260805-0800"" />
                             <high value=""200802281205-0800"" />
                         </effectiveTime>
                         <value type=""CD"" code=""422587007"" codeSystem=""2.16.840.1.113883.6.96"" displayName=""Nausea"" />
                     </observation>
                     ";
                     var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;

                     var attributes = new Dictionary<string, object>
                     {
                         { "ID", "1234" },
                         { "adverseEventEntry", parsed["observation"]},
                     };
            var actualFhir = GetFhirObjectFromTemplate<AdverseEvent>(ECRPath, attributes);

            Assert.Equal("AdverseEvent", actualFhir.TypeName);
//            Assert.Equal(AdverseEvent.AdverseEventIntent.Plan, actualFhir.Intent);
            Assert.Equal("1234" ,actualFhir.Id);
//            Assert.Equal(AdverseEvent.MedicationrequestStatus.Active, actualFhir.Status);
//            Assert.Equal("Why not", actualFhir.ReasonCode.First().Coding.First().Code);
//            Assert.Equal(RequestPriority.Asap , actualFhir.Priority);
//            Assert.Equal(1, actualFhir.DispenseRequest.NumberOfRepeatsAllowed);
//            var dosage = actualFhir.DosageInstruction.First();
//            Assert.NotEmpty(dosage.Site);
//            Assert.NotEmpty(dosage.DoseAndRate);
//            Assert.NotEmpty(dosage.Route);
        }
    }
}
