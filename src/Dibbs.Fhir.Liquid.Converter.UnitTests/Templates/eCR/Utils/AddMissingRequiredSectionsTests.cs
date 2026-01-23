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
    public class AddMissingRequiredSectionsTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory, "Utils", "_AddMissingRequiredSections.liquid"
        );

        [Fact]
        public void AddsMissingRequiredSection()
        {
            // Composition is missing one required section: Chief Complaint
            var templatesinComp = "2.16.840.1.113883.10.20.22.2.10|2.16.840.1.113883.10.20.22.2.22|2.16.840.1.113883.10.20.22.2.22.1|1.3.6.1.4.1.19376.1.5.3.1.3.4|2.16.840.1.113883.10.20.22.2.38|2.16.840.1.113883.10.20.22.2.5|2.16.840.1.113883.10.20.22.2.5.1|2.16.840.1.113883.10.20.22.2.12|2.16.840.1.113883.10.20.22.2.3|2.16.840.1.113883.10.20.22.2.3.1|2.16.840.1.113883.10.20.22.2.7.1|2.16.840.1.113883.10.20.22.2.2.1|2.16.840.1.113883.10.20.22.2.17|2.16.840.1.113883.10.20.22.2.80|2.16.840.1.113883.10.20.22.2.4.1|2.16.840.1.113883.10.20.15.2.2.4";
            var expected = "{ \"code\": { \"coding\": [ { \"system\": \"http://loinc.org\", \"code\": \"10154-3\", \"display\": \"Chief complaint Narrative - Reported\", } ] }, \"text\": { \"status\": \"empty\", \"div\": \"<div xmlns=\\\"http://www.w3.org/1999/xhtml\\\">No Information Available</div>\", } },";

            var attributes = new Dictionary<string, object>
            {
                { "templatesInComp", templatesinComp},
            };

            ConvertCheckLiquidTemplate(ECRPath, attributes, expected);
        }

        [Fact]
        public void NoMissingRequiredSections()
        {
            // Composition had all required sections, none are missing
            var templatesinComp = "2.16.840.1.113883.10.20.22.2.10|2.16.840.1.113883.10.20.22.2.22|2.16.840.1.113883.10.20.22.2.22.1|1.3.6.1.4.1.19376.1.5.3.1.3.4|2.16.840.1.113883.10.20.22.2.38|2.16.840.1.113883.10.20.22.2.5|2.16.840.1.113883.10.20.22.2.5.1|1.3.6.1.4.1.19376.1.5.3.1.1.13.2.1|2.16.840.1.113883.10.20.22.2.12|2.16.840.1.113883.10.20.22.2.3|2.16.840.1.113883.10.20.22.2.3.1|2.16.840.1.113883.10.20.22.2.7.1|2.16.840.1.113883.10.20.22.2.2.1|2.16.840.1.113883.10.20.22.2.17|2.16.840.1.113883.10.20.22.2.80|2.16.840.1.113883.10.20.22.2.4.1|2.16.840.1.113883.10.20.15.2.2.4";

            var attributes = new Dictionary<string, object>
            {
                { "templatesInComp", templatesinComp},
            };

            ConvertCheckLiquidTemplate(ECRPath, attributes, "");
        }
    }
}
