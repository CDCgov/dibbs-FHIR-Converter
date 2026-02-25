using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hl7.Fhir.Model;
using Xunit;
using Dibbs.Fhir.Liquid.Converter.DataParsers;
using System;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests
{
    public class RelatedPersonTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "Resource",
            "RelatedPerson.liquid"
        );

        [Fact]
        public void RelatedPerson_Guardian()
        {
            // from 3.1 spec
            var xmlStr = @"
            <guardian 
                xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                xsi:schemaLocation=""urn:hl7-org:v3 ../../../cda-core-2.0/schema/extensions/SDTC/infrastructure/cda/CDA_SDTC.xsd""
                xmlns=""urn:hl7-org:v3""
                xmlns:cda=""urn:hl7-org:v3""
                xmlns:sdtc=""urn:hl7-org:sdtc""
                xmlns:voc=""http://www.lantanagroup.com/voc""
                >
                <code code=""POWATT"" displayName=""Power of Attorney""
                    codeSystem=""2.16.840.1.113883.1.11.19830"" codeSystemName=""ResponsibleParty"" />
                <addr use=""HP"">
                    <streetAddressLine>2222 Home Street</streetAddressLine>
                    <city>Beaverton</city>
                    CDA® R2 Public Health Case Report - eICR Release 1, STU Release 3.1: Templates and Supporting Page 61
                    July 2022 © 2022 Health Level Seven International All rights reserved.
                    <state>OR</state>
                    <postalCode>97867</postalCode>
                    <country>US</country>
                </addr>
                <telecom value=""tel:+1(555)555-2008"" use=""MC"" />
                <guardianPerson>
                    <name>
                        <given>Boris</given>
                        <given qualifier=""CL"">Bo</given>
                        <family>Betterhalf</family>
                    </name>
                </guardianPerson>
            </guardian>
            ";
            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "relatedPerson", parsed["guardian"]},
            };

            var actualFhir = GetFhirObjectFromTemplate<RelatedPerson>(ECRPath, attributes);

            Assert.Equal(ResourceType.RelatedPerson.ToString(), actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);

            var rels = actualFhir.Relationship;
            Assert.Equal(2, actualFhir.Relationship.Count());
            Assert.Equal("Power of Attorney", rels[0].Coding[0].Display);
            Assert.Equal("Guardian", rels[1].Coding[0].Display);

            Assert.Equal("Betterhalf", actualFhir.Name[0].Family);

            Assert.Equal("+1(555)555-2008", actualFhir.Telecom[0].Value);
            Assert.Equal("Phone", actualFhir.Telecom[0].System.ToString());
            Assert.Equal("Mobile", actualFhir.Telecom[0].Use.ToString());

            Assert.Equal("Beaverton", actualFhir.Address[0].City);
        }
    }
}
