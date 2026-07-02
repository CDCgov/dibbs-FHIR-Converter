using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hl7.Fhir.Model;
using Xunit;
using Dibbs.Fhir.Liquid.Converter.DataParsers;
using System;


namespace Dibbs.Fhir.Liquid.Converter.UnitTests
{
    public class PatientTests : BaseECRLiquidTests
    {
        private static readonly string ECRPath = Path.Join(
            TestConstants.ECRTemplateDirectory,
            "Resource",
            "Patient.liquid"
        );

        [Fact]
        public void Patient_AllFields()
        {
            // From Eve Everywoman
            // Added maritalStatus from Dash Rendar
            // Added synthetic religiousAfifiliationCode
            var xmlStr = @"
                <patientRole
                    xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                    xsi:schemaLocation=""urn:hl7-org:v3 ../../../cda-core-2.0/schema/extensions/SDTC/infrastructure/cda/CDA_SDTC.xsd""
                    xmlns=""urn:hl7-org:v3""
                    xmlns:cda=""urn:hl7-org:v3""
                    xmlns:sdtc=""urn:hl7-org:sdtc""
                    xmlns:voc=""http://www.lantanagroup.com/voc""
                >
                    <!-- Fake root for sample -->
                    <id extension=""123453"" root=""2.16.840.1.113883.19.5"" />
                    <!--SSN-->
                    <id extension=""444-22-2222"" root=""2.16.840.1.113883.4.1"" />
                    <!-- For greatest utility to public health, a patient's address 
                should be a home address if available (PostalAddressUse = 'H' or 'HP'); 
                would also request a second address, preferably a work address, (PostalAddressUse='WP') if
                    available. 
                If the patient is homeless, complete as much address information 
                as possible (city, zip, county, etc.) and use the 
                Characteristics of Home Environment template in the 
                Social History Section to indicate that the patient is homeless. -->
                    <addr use=""H"">
                        <streetAddressLine>2222 Home Street</streetAddressLine>
                        <city>Ann Arbor</city>
                        <state>MI</state>
                        <postalCode>99999</postalCode>
                        <!-- Although ""county"" is not explicitly specified in the 
                    US Realm Address, it is not precluded from use and for 
                    the purposes of this IG it SHOULD be included. 
                    See the IG for more information. -->
                        <county>26001</county>
                        <country>US</country>
                        <!-- usablePeriod is an optional element
                                If present and high is missing, this indicates a current addres
                                If present and high is present, this indicates this address is historical
                                If present, low indicates the starting period for this address 
                                The following example indicates a current address-->
                        <useablePeriod xsi:type=""IVL_TS"">
                            <low value=""200007200845"" />
                        </useablePeriod>
                    </addr>
                    <!-- Patient Telcom (phone, email, or fax) -->
                    <telecom use=""HP"" value=""tel:+1-555-555-2003"" />
                    <telecom use=""WP"" value=""tel:+1-555-555-2004"" />
                    <!-- Patient Name -->
                    <patient>
                        <!-- Patient ""legal"" (known as/conventional/the one you use) name -->
                        <name use=""L"">
                            <given>Eve</given>
                            <given qualifier=""IN"">H</given>
                            <family>Everywoman</family>
                        </name>
                        <!-- Patient ""artist/stage"" (includes writer's pseudonym, stage name, etc) name -->
                        <name use=""A"">
                            <given>Ruth</given>
                            <given qualifier=""IN"">L</given>
                            <family>Everywoman</family>
                        </name>
                        <administrativeGenderCode code=""F"" codeSystem=""2.16.840.1.113883.5.1"" />
                        <!-- Patient Birthdate -->
                        <birthTime value=""19741124"" />
                        <!-- If sdtc:deceasedInd is true then sdtc:deceasedTime must be present -->
                        <sdtc:deceasedInd value=""false"" />
                        <!-- Patient Race -->
                        <raceCode code=""2106-3"" codeSystem=""2.16.840.1.113883.6.238""
                            codeSystemName=""Race &amp; Ethnicity - CDC"" displayName=""White"" />
                        <!-- Patient Ethnicity -->
                        <ethnicGroupCode code=""2186-5"" codeSystem=""2.16.840.1.113883.6.238""
                            codeSystemName=""Race &amp; Ethnicity - CDC"" displayName=""Not Hispanic or Latino"" />
                        <!-- Parent/Guardian information-->
                        <guardian>
                            <!-- Parent/Guardian Address -->
                            <addr use=""H"">
                                <streetAddressLine>4444 Home Street</streetAddressLine>
                                <city>Ann Arbor</city>
                                <state>MI</state>
                                <postalCode>99999</postalCode>
                                <country>US</country>
                            </addr>
                            <!-- Parent/Guardian phone -->
                            <telecom use=""HP"" value=""tel:+1-555-555-2006"" />
                            <!-- Parent/Guardian email -->
                            <telecom value=""mailto:mail@guardian.com"" />
                            <guardianPerson>
                                <!-- Parent/guardian name -->
                                <name use=""L"">
                                    <given>Martha</given>
                                    <given qualifier=""IN"">L</given>
                                    <family>Mum</family>
                                </name>
                            </guardianPerson>
                        </guardian>
                        <languageCommunication>
                            <languageCode code=""en"" />
                            <modeCode code=""ESP"" codeSystem=""2.16.840.1.113883.5.60""
                                codeSystemName=""LanguageAbilityMode"" displayName=""Expressed spoken"" />
                            <proficiencyLevelCode code=""G"" codeSystem=""2.16.840.1.113883.5.61""
                                codeSystemName=""LanguageAbilityProficiency"" displayName=""Good"" />
                            <!-- Preferred Language -->
                            <preferenceInd value=""true"" />
                        </languageCommunication>
                        <!-- ADDED: martialStatus from Dash Rendar -->
                        <maritalStatusCode code=""M"" displayName=""Married""
                            codeSystem=""2.16.840.1.113883.1.11.12212""
                            codeSystemName=""MaritalStatusCode"" />
                        <!-- ADDED: religiousAffiliationCode (synthetic) -->
                        <religiousAffiliationCode code=""Catholic""
                            codeSystem=""2.16.840.1.113883.5.1076"" 
                            codeSystemName=""ReligiousAffiliation"" />

                    </patient>
                </patientRole>
            ";
            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "patientRole", parsed["patientRole"]},
            };

            var actualFhir = GetFhirObjectFromTemplate<Patient>(ECRPath, attributes);

            Assert.Equal(ResourceType.Patient.ToString(), actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);

            Assert.Equal("http://hl7.org/fhir/us/core/StructureDefinition/us-core-patient", actualFhir.Meta.ProfileElement[0].Value);
            Assert.Equal("123453", actualFhir.Identifier[0].Value);
            Assert.Equal("444-22-2222", actualFhir.Identifier[1].Value);

            // Name
            Assert.Equal(HumanName.NameUse.Official, actualFhir.Name[0].Use);
            Assert.Equal("Everywoman", actualFhir.Name[0].Family);
            Assert.Equal(new[] { "Eve", "H" }, actualFhir.Name[0].Given);
            Assert.Equal(new[] {""}, actualFhir.Name[0].Prefix);
            Assert.Equal(new[] {""}, actualFhir.Name[0].Suffix);

            Assert.Equal(HumanName.NameUse.Usual, actualFhir.Name[1].Use);
            Assert.Equal("Everywoman", actualFhir.Name[1].Family);
            Assert.Equal(new[] { "Ruth", "L" }, actualFhir.Name[1].Given);
            Assert.Equal(new[] {""}, actualFhir.Name[1].Prefix);
            Assert.Equal(new[] {""}, actualFhir.Name[1].Suffix);

            // Telecom
            Assert.Equal("+1-555-555-2003", actualFhir.Telecom[0].Value);
            Assert.Equal("Phone", actualFhir.Telecom[0].System.ToString());
            Assert.Equal("Home", actualFhir.Telecom[0].Use.ToString());

            Assert.Equal("+1-555-555-2004", actualFhir.Telecom[1].Value);
            Assert.Equal("Phone", actualFhir.Telecom[1].System.ToString());
            Assert.Equal("Work", actualFhir.Telecom[1].Use.ToString());

            // Address
            Assert.Equal(Address.AddressUse.Home, actualFhir.Address[0].Use);
            Assert.Equal(new [] {"2222 Home Street"}, actualFhir.Address[0].Line);
            Assert.Equal("Ann Arbor", actualFhir.Address[0].City);
            Assert.Equal("26001", actualFhir.Address[0].District);
            Assert.Equal("MI", actualFhir.Address[0].State);
            Assert.Equal("99999", actualFhir.Address[0].PostalCode);
            Assert.Equal("US", actualFhir.Address[0].Country);
            Assert.Equal("2000-07-20T08:45:00", actualFhir.Address[0].Period.Start);
            Assert.Equal("", actualFhir.Address[0].Period.End);

            // Communication
            Assert.Equal("English", actualFhir.Communication[0].Language.Coding[0].Display);
            Assert.Equal("en", actualFhir.Communication[0].Language.Coding[0].Code);
            Assert.Equal("urn:ietf:bcp:47", actualFhir.Communication[0].Language.Coding[0].System);
            Assert.Equal(true, actualFhir.Communication[0].Preferred.Value);
        
            Assert.Equal("http://hl7.org/fhir/StructureDefinition/patient-proficiency", actualFhir.Communication[0].Extension[0].Url);

            Assert.Equal("type", actualFhir.Communication[0].Extension[0].Extension[0].Url);
            var commType = (Coding)actualFhir.Communication[0].Extension[0].Extension[0].Value;
            Assert.Equal("http://terminology.hl7.org/CodeSystem/v3-LanguageAbilityMode", commType.System);
            Assert.Equal("ESP", commType.Code);
            Assert.Equal("Expressed spoken", commType.Display);

            Assert.Equal("level", actualFhir.Communication[0].Extension[0].Extension[1].Url);
            var commLevel = (Coding)actualFhir.Communication[0].Extension[0].Extension[1].Value;
            Assert.Equal("http://terminology.hl7.org/CodeSystem/v3-LanguageAbilityProficiency", commLevel.System);
            Assert.Equal("G", commLevel.Code);
            Assert.Equal("Good", commLevel.Display);

            // Race and ethnicity
            Assert.Equal("http://hl7.org/fhir/us/core/StructureDefinition/us-core-race", actualFhir.Extension[0].Url);
            Assert.Equal("ombCategory", actualFhir.Extension[0].Extension[0].Url);
            var race = (Coding)actualFhir.Extension[0].Extension[0].Value;
            Assert.Equal("urn:oid:2.16.840.1.113883.6.238", race.System);
            Assert.Equal("2106-3", race.Code);
            Assert.Equal("White", race.Display);
            Assert.Equal("text", actualFhir.Extension[0].Extension[1].Url);
            Assert.Equal("White", ((FhirString)actualFhir.Extension[0].Extension[1].Value).Value);

            Assert.Equal("http://hl7.org/fhir/us/core/StructureDefinition/us-core-ethnicity", actualFhir.Extension[1].Url);
            Assert.Equal("ombCategory", actualFhir.Extension[1].Extension[0].Url);
            var ethnicity = (Coding)actualFhir.Extension[1].Extension[0].Value;
            Assert.Equal("urn:oid:2.16.840.1.113883.6.238", ethnicity.System);
            Assert.Equal("2186-5", ethnicity.Code);
            Assert.Equal("Non Hispanic or Latino", ethnicity.Display);
            Assert.Equal("text", actualFhir.Extension[1].Extension[1].Url);
            Assert.Equal("Not Hispanic or Latino", ((FhirString)actualFhir.Extension[1].Extension[1].Value).Value);

            // Other
            Assert.Equal(AdministrativeGender.Female, actualFhir.Gender);
            Assert.Equal("1974-11-24", actualFhir.BirthDate);
            Assert.Equal("urn:oid:2.16.840.1.113883.5.1076", ((CodeableConcept)actualFhir.Extension[2].Value).Coding[0].System);
            Assert.Equal("Catholic", ((CodeableConcept)actualFhir.Extension[2].Value).Coding[0].Code);
            Assert.Equal("http://hl7.org/fhir/us/core/StructureDefinition/us-core-birthsex", actualFhir.Extension[3].Url);
            Assert.Equal("UNK", ((Code)actualFhir.Extension[3].Value).Value);
            Assert.Equal(false, ((FhirBoolean)actualFhir.Deceased).Value);
            Assert.Equal("Married", actualFhir.MaritalStatus.Coding[0].Display);
        }

        [Fact]
        public void Patient_IsDeceased()
        {
            var xmlStr = @"
                <patientRole
                    xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
                    xsi:schemaLocation=""urn:hl7-org:v3 ../../../cda-core-2.0/schema/extensions/SDTC/infrastructure/cda/CDA_SDTC.xsd""
                    xmlns=""urn:hl7-org:v3""
                    xmlns:cda=""urn:hl7-org:v3""
                    xmlns:sdtc=""urn:hl7-org:sdtc""
                    xmlns:voc=""http://www.lantanagroup.com/voc""
                >
                    <patient>
                        <sdtc:deceasedInd value=""true"" />
                        <sdtc:deceasedTime value=""20260701"" />
                    </patient>
                </patientRole>
            ";
            var parsed = new CcdaDataParser().Parse(xmlStr) as Dictionary<string, object>;

            var attributes = new Dictionary<string, object>
            {
                { "ID", "1234" },
                { "patientRole", parsed["patientRole"]},
            };

            var actualFhir = GetFhirObjectFromTemplate<Patient>(ECRPath, attributes);

            Assert.Equal(ResourceType.Patient.ToString(), actualFhir.TypeName);
            Assert.NotNull(actualFhir.Id);

            Assert.Equal("2026-07-01", ((Date)actualFhir.Deceased).Value);
        }
    }
}
