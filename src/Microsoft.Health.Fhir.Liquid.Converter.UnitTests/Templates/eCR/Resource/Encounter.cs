using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotLiquid;
using Xunit;
using Hl7.Fhir.Model;
using Microsoft.Health.Fhir.Liquid.Converter.Parsers;
using System;

namespace Microsoft.Health.Fhir.Liquid.Converter.UnitTests
{
    public class EncounterTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory, "Resource", "_Encounter.liquid"
        );

        [Fact]
        public void Encounter_Basic_AllFields()
        {
            // from 3.1 Eve Everywoman
            // Missing: priority, hospitalization
            var xmlStr = @"
                <encompassingEncounter>
                    <id extension=""9937012"" root=""2.16.840.1.113883.19"" />
                    <code code=""AMB"" codeSystem=""2.16.840.1.113883.5.4""
                        codeSystemName=""HL7 ActEncounterCode"" displayName=""Ambulatory"" />
                    <effectiveTime>
                        <!-- Admission Date/Time (inpatient) OR Visit Date/Time (outpatient) -->
                        <low value=""20201107084421-0500"" />
                        <!-- Discharge Date/Time (if missing, the encounter is still in progress)  -->
                        <high value=""20201108112103-0500"" />
                    </effectiveTime>
                </encompassingEncounter>
            ";
            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "encounter", parsed["encompassingEncounter"]},
            };

            var actualFhir = GetFhirObjectFromTemplate<Encounter>(ECRPath, attributes);

            Assert.Equal(ResourceType.Encounter.ToString(), actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);
            Assert.Equal("Finished", actualFhir.Status.ToString());

            Assert.Equal("urn:oid:2.16.840.1.113883.19", actualFhir.Identifier.First().System);
            Assert.Equal("9937012", actualFhir.Identifier.First().Value);

            Assert.Equal("AMB", actualFhir.Class.Code);
            Assert.Equal("urn:oid:2.16.840.1.113883.5.4", actualFhir.Class.System);
            Assert.Equal("Ambulatory", actualFhir.Class.Display);

            Assert.Equal("2020-11-07T08:44:21-05:00", actualFhir.Period.Start);
            Assert.Equal("2020-11-08T11:21:03-05:00", actualFhir.Period.End);
        }

    }
}
