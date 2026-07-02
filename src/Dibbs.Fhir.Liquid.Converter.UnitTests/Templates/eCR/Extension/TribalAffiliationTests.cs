using System.Collections.Generic;
using System.IO;
using Xunit;
using Dibbs.Fhir.Liquid.Converter.DataParsers;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests
{
    public class TribalAffiliationTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "Extension",
            "TribalAffiliation.liquid"
        );

        [Fact]
        public async void TribalAffiliation_AllFields()
        {
            // From eve_everywoman_fully_processed
            var xmlStr = @"
                <observation
                    xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                    xsi:schemaLocation=""urn:hl7-org:v3 ../../../cda-core-2.0/schema/extensions/SDTC/infrastructure/cda/CDA_SDTC.xsd""
                    xmlns=""urn:hl7-org:v3""
                    xmlns:cda=""urn:hl7-org:v3""
                    xmlns:sdtc=""urn:hl7-org:sdtc""
                    xmlns:voc=""http://www.lantanagroup.com/voc""
                    classCode=""OBS""
                    moodCode=""EVN""
                >
                    <!-- [eICR R2 STU3] Tribal Affiliation Observation -->
                    <templateId root=""2.16.840.1.113883.10.20.15.2.3.48""
                        extension=""2021-01-01"" />
                    <id root=""cecfb1ba-1e13-47bd-b6ea-5a40f22798a9"" />
                    <!-- Tribe name -->
                    <code code=""91"" codeSystem=""2.16.840.1.113883.5.140""
                        displayName=""Fort Mojave Indian Tribe of Arizona, California""
                        codeSystemName=""TribalEntityUS"" />
                    <statusCode code=""completed"" />
                    <effectiveTime value=""20201109"" />
                    <!-- Enrolled Tribe Member -->
                    <value xsi:type=""BL"" value=""true"" />
                </observation>
            ";
            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;
            var attributes = new Dictionary<string, object> {{ "tribeAOb", parsed["observation"]}};

            await ConvertCheckLiquidTemplate(
                ECRPath,
                attributes,
                @"""url"": ""http://hl7.org/fhir/us/ecr/StructureDefinition/us-ph-tribal-affiliation-extension"", ""extension"": [ { ""url"": ""TribeName"", ""valueCoding"": { ""code"": ""91"",""system"": ""http://terminology.hl7.org/CodeSystem/v3-TribalEntityUS"",""display"": ""Fort Mojave Indian Tribe of Arizona, California"",}, }, { ""url"": ""EnrolledTribeMember"", ""valueBoolean"": true, }, ],");
        }
    }
}
