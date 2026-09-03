using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hl7.Fhir.Model;
using Xunit;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests
{
    public class SpecimenProcedureTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "Resource",
            "SpecimenProcedure.liquid"
        );

        [Fact]
        public void SpecimenProcedure_UsesParticipantRoleAndProcedureIds()
        {
            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                {
                    "specimenProc",
                    new
                    {
                        id = new object[]
                        {
                            new { root = "ab1791b0-5c71-11db-b0de-0800200c9a57" },
                            new { root = "ab1791b0-5c71-11db-b0de-0800200c9a58" },
                        },
                        participant = new
                        {
                            participantRole = new
                            {
                                id = new object[]
                                {
                                    new { root = "ab1791b0-5c71-11db-b0de-0800200c9a55" },
                                    new { root = "ab1791b0-5c71-11db-b0de-0800200c9a56" },
                                },
                                playingEntity = new
                                {
                                    code = new { code = "119297000" },
                                },
                            },
                        },
                        entryRelationship = new object[] { },
                    }
                },
            };

            var actualFhir = GetFhirObjectFromTemplate<Specimen>(ECRPath, attributes);

            Assert.Equal(
                new[]
                {
                    "urn:uuid:ab1791b0-5c71-11db-b0de-0800200c9a55",
                    "urn:uuid:ab1791b0-5c71-11db-b0de-0800200c9a56",
                    "urn:uuid:ab1791b0-5c71-11db-b0de-0800200c9a57",
                    "urn:uuid:ab1791b0-5c71-11db-b0de-0800200c9a58",
                },
                actualFhir.Identifier.Select(identifier => identifier.Value));
        }
    }
}
