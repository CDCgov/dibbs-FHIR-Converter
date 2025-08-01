﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Health.Fhir.Liquid.Converter.Models;
using Microsoft.Health.Fhir.Liquid.Converter.Models.Hl7v2;
using Microsoft.Health.Fhir.Liquid.Converter.Processors;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

using Firely.Fhir.Packages;
using Firely.Fhir.Validation;
using Firely.Fhir.Validation.Compilation;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Specification.Terminology;
using Hl7.Fhir.Support;
using Hl7.Fhir.Utility;

namespace Microsoft.Health.Fhir.Liquid.Converter.FunctionalTests
{
    public class BaseConvertDataFunctionalTests
    {
        private static readonly ProcessorSettings _processorSettings = new ProcessorSettings();

        public static IEnumerable<object[]> GetDataForHl7v2()
        {
            var data = new List<string[]>
            {
                new[] { @"ADT_A01", @"ADT-A01-01.hl7", @"ADT-A01-01-expected.json" },
                new[] { @"ADT_A01", @"ADT-A01-02.hl7", @"ADT-A01-02-expected.json" },
                new[] { @"ADT_A02", @"ADT-A02-01.hl7", @"ADT-A02-01-expected.json" },
                new[] { @"ADT_A02", @"ADT-A02-02.hl7", @"ADT-A02-02-expected.json" },
                new[] { @"ADT_A03", @"ADT-A03-01.hl7", @"ADT-A03-01-expected.json" },
                new[] { @"ADT_A03", @"ADT-A03-02.hl7", @"ADT-A03-02-expected.json" },
                new[] { @"ADT_A04", @"ADT-A04-01.hl7", @"ADT-A04-01-expected.json" },
                new[] { @"ADT_A04", @"ADT-A04-02.hl7", @"ADT-A04-02-expected.json" },
                new[] { @"ADT_A05", @"ADT-A05-01.hl7", @"ADT-A05-01-expected.json" },
                new[] { @"ADT_A05", @"ADT-A05-02.hl7", @"ADT-A05-02-expected.json" },
                new[] { @"ADT_A06", @"ADT-A06-01.hl7", @"ADT-A06-01-expected.json" },
                new[] { @"ADT_A06", @"ADT-A06-02.hl7", @"ADT-A06-02-expected.json" },
                new[] { @"ADT_A07", @"ADT-A07-01.hl7", @"ADT-A07-01-expected.json" },
                new[] { @"ADT_A07", @"ADT-A07-02.hl7", @"ADT-A07-02-expected.json" },
                new[] { @"ADT_A08", @"ADT-A08-01.hl7", @"ADT-A08-01-expected.json" },
                new[] { @"ADT_A08", @"ADT-A08-02.hl7", @"ADT-A08-02-expected.json" },
                new[] { @"ADT_A09", @"ADT-A09-01.hl7", @"ADT-A09-01-expected.json" },
                new[] { @"ADT_A09", @"ADT-A09-02.hl7", @"ADT-A09-02-expected.json" },
                new[] { @"ADT_A10", @"ADT-A10-01.hl7", @"ADT-A10-01-expected.json" },
                new[] { @"ADT_A10", @"ADT-A10-02.hl7", @"ADT-A10-02-expected.json" },
                new[] { @"ADT_A11", @"ADT-A11-01.hl7", @"ADT-A11-01-expected.json" },
                new[] { @"ADT_A11", @"ADT-A11-02.hl7", @"ADT-A11-02-expected.json" },
                new[] { @"ADT_A13", @"ADT-A13-01.hl7", @"ADT-A13-01-expected.json" },
                new[] { @"ADT_A13", @"ADT-A13-02.hl7", @"ADT-A13-02-expected.json" },
                new[] { @"ADT_A14", @"ADT-A14-01.hl7", @"ADT-A14-01-expected.json" },
                new[] { @"ADT_A14", @"ADT-A14-02.hl7", @"ADT-A14-02-expected.json" },
                new[] { @"ADT_A15", @"ADT-A15-01.hl7", @"ADT-A15-01-expected.json" },
                new[] { @"ADT_A15", @"ADT-A15-02.hl7", @"ADT-A15-02-expected.json" },
                new[] { @"ADT_A16", @"ADT-A16-01.hl7", @"ADT-A16-01-expected.json" },
                new[] { @"ADT_A16", @"ADT-A16-02.hl7", @"ADT-A16-02-expected.json" },
                new[] { @"ADT_A25", @"ADT-A25-01.hl7", @"ADT-A25-01-expected.json" },
                new[] { @"ADT_A25", @"ADT-A25-02.hl7", @"ADT-A25-02-expected.json" },
                new[] { @"ADT_A26", @"ADT-A26-01.hl7", @"ADT-A26-01-expected.json" },
                new[] { @"ADT_A26", @"ADT-A26-02.hl7", @"ADT-A26-02-expected.json" },
                new[] { @"ADT_A27", @"ADT-A27-01.hl7", @"ADT-A27-01-expected.json" },
                new[] { @"ADT_A27", @"ADT-A27-02.hl7", @"ADT-A27-02-expected.json" },
                new[] { @"ADT_A28", @"ADT-A28-01.hl7", @"ADT-A28-01-expected.json" },
                new[] { @"ADT_A28", @"ADT-A28-02.hl7", @"ADT-A28-02-expected.json" },
                new[] { @"ADT_A29", @"ADT-A29-01.hl7", @"ADT-A29-01-expected.json" },
                new[] { @"ADT_A29", @"ADT-A29-02.hl7", @"ADT-A29-02-expected.json" },
                new[] { @"ADT_A31", @"ADT-A31-01.hl7", @"ADT-A31-01-expected.json" },
                new[] { @"ADT_A31", @"ADT-A31-02.hl7", @"ADT-A31-02-expected.json" },
                new[] { @"ADT_A40", @"ADT-A40-01.hl7", @"ADT-A40-01-expected.json" },
                new[] { @"ADT_A40", @"ADT-A40-02.hl7", @"ADT-A40-02-expected.json" },
                new[] { @"ADT_A41", @"ADT-A41-01.hl7", @"ADT-A41-01-expected.json" },
                new[] { @"ADT_A41", @"ADT-A41-02.hl7", @"ADT-A41-02-expected.json" },
                new[] { @"ADT_A45", @"ADT-A45-01.hl7", @"ADT-A45-01-expected.json" },
                new[] { @"ADT_A45", @"ADT-A45-02.hl7", @"ADT-A45-02-expected.json" },
                new[] { @"ADT_A47", @"ADT-A47-01.hl7", @"ADT-A47-01-expected.json" },
                new[] { @"ADT_A47", @"ADT-A47-02.hl7", @"ADT-A47-02-expected.json" },
                new[] { @"ADT_A60", @"ADT-A60-01.hl7", @"ADT-A60-01-expected.json" },
                new[] { @"ADT_A60", @"ADT-A60-02.hl7", @"ADT-A60-02-expected.json" },

                new[] { @"SIU_S12", @"SIU-S12-01.hl7", @"SIU-S12-01-expected.json" },
                new[] { @"SIU_S12", @"SIU-S12-02.hl7", @"SIU-S12-02-expected.json" },
                new[] { @"SIU_S13", @"SIU-S13-01.hl7", @"SIU-S13-01-expected.json" },
                new[] { @"SIU_S13", @"SIU-S13-02.hl7", @"SIU-S13-02-expected.json" },
                new[] { @"SIU_S14", @"SIU-S14-01.hl7", @"SIU-S14-01-expected.json" },
                new[] { @"SIU_S14", @"SIU-S14-02.hl7", @"SIU-S14-02-expected.json" },
                new[] { @"SIU_S15", @"SIU-S15-01.hl7", @"SIU-S15-01-expected.json" },
                new[] { @"SIU_S15", @"SIU-S15-02.hl7", @"SIU-S15-02-expected.json" },
                new[] { @"SIU_S16", @"SIU-S16-01.hl7", @"SIU-S16-01-expected.json" },
                new[] { @"SIU_S16", @"SIU-S16-02.hl7", @"SIU-S16-02-expected.json" },
                new[] { @"SIU_S17", @"SIU-S17-01.hl7", @"SIU-S17-01-expected.json" },
                new[] { @"SIU_S17", @"SIU-S17-02.hl7", @"SIU-S17-02-expected.json" },
                new[] { @"SIU_S26", @"SIU-S26-01.hl7", @"SIU-S26-01-expected.json" },
                new[] { @"SIU_S26", @"SIU-S26-02.hl7", @"SIU-S26-02-expected.json" },

                new[] { @"ORU_R01", @"ORU-R01-01.hl7",  @"ORU-R01-01-expected.json" },

                new[] { @"ORM_O01", @"ORM-O01-01.hl7", @"ORM-O01-01-expected.json" },
                new[] { @"ORM_O01", @"ORM-O01-02.hl7", @"ORM-O01-02-expected.json" },
                new[] { @"ORM_O01", @"ORM-O01-03.hl7", @"ORM-O01-03-expected.json" },
                new[] { @"ORM_O01", @"ORM-O01-04.hl7", @"ORM-O01-04-expected.json" },
                new[] { @"ORM_O01", @"ORM-O01-05.hl7", @"ORM-O01-05-expected.json" },
                new[] { @"ORM_O01", @"ORM-O01-06.hl7", @"ORM-O01-06-expected.json" },

                new[] { @"MDM_T01", @"MDM-T01-01.hl7",  @"MDM-T01-01-expected.json" },
                new[] { @"MDM_T01", @"MDM-T01-02.hl7",  @"MDM-T01-02-expected.json" },
                new[] { @"MDM_T02", @"MDM-T02-01.hl7",  @"MDM-T02-01-expected.json" },
                new[] { @"MDM_T02", @"MDM-T02-02.hl7",  @"MDM-T02-02-expected.json" },
                new[] { @"MDM_T02", @"MDM-T02-03.hl7",  @"MDM-T02-03-expected.json" },
                new[] { @"MDM_T05", @"MDM-T05-01.hl7",  @"MDM-T05-01-expected.json" },
                new[] { @"MDM_T05", @"MDM-T05-02.hl7",  @"MDM-T05-02-expected.json" },
                new[] { @"MDM_T06", @"MDM-T06-01.hl7",  @"MDM-T06-01-expected.json" },
                new[] { @"MDM_T06", @"MDM-T06-02.hl7",  @"MDM-T06-02-expected.json" },
                new[] { @"MDM_T09", @"MDM-T09-01.hl7",  @"MDM-T09-01-expected.json" },
                new[] { @"MDM_T09", @"MDM-T09-02.hl7",  @"MDM-T09-02-expected.json" },
                new[] { @"MDM_T10", @"MDM-T10-01.hl7",  @"MDM-T10-01-expected.json" },
                new[] { @"MDM_T10", @"MDM-T10-02.hl7",  @"MDM-T10-02-expected.json" },

                new[] { @"RDE_O11", @"RDE-O11-01.hl7", @"RDE-O11-01-expected.json" },
                new[] { @"RDE_O11", @"RDE-O11-02.hl7", @"RDE-O11-02-expected.json" },
                new[] { @"RDE_O25", @"RDE-O25-01.hl7", @"RDE-O25-01-expected.json" },
                new[] { @"RDE_O25", @"RDE-O25-02.hl7", @"RDE-O25-02-expected.json" },

                new[] { @"RDS_O13", @"RDS-O13-01.hl7", @"RDS-O13-01-expected.json" },
                new[] { @"RDS_O13", @"RDS-O13-02.hl7", @"RDS-O13-02-expected.json" },

                new[] { @"OML_O21", @"OML-O21-01.hl7",  @"OML-O21-01-expected.json" },
                new[] { @"OML_O21", @"OML-O21-02.hl7",  @"OML-O21-02-expected.json" },
                new[] { @"OML_O21", @"OML-O21-03.hl7",  @"OML-O21-03-expected.json" },

                new[] { @"OUL_R22", @"OUL-R22-01.hl7",  @"OUL-R22-01-expected.json" },
                new[] { @"OUL_R22", @"OUL-R22-02.hl7",  @"OUL-R22-02-expected.json" },
                new[] { @"OUL_R23", @"OUL-R23-01.hl7",  @"OUL-R23-01-expected.json" },
                new[] { @"OUL_R23", @"OUL-R23-02.hl7",  @"OUL-R23-02-expected.json" },
                new[] { @"OUL_R24", @"OUL-R24-01.hl7",  @"OUL-R24-01-expected.json" },
                new[] { @"OUL_R24", @"OUL-R24-02.hl7",  @"OUL-R24-02-expected.json" },

                new[] { @"VXU_V04", @"VXU-V04-01.hl7",  @"VXU-V04-01-expected.json" },
                new[] { @"VXU_V04", @"VXU-V04-02.hl7",  @"VXU-V04-02-expected.json" },

                new[] { @"BAR_P01", @"BAR-P01-01.hl7", @"BAR-P01-01-expected.json" },
                new[] { @"BAR_P01", @"BAR-P01-02.hl7", @"BAR-P01-02-expected.json" },
                new[] { @"BAR_P02", @"BAR-P02-01.hl7", @"BAR-P02-01-expected.json" },
                new[] { @"BAR_P02", @"BAR-P02-02.hl7", @"BAR-P02-02-expected.json" },
                new[] { @"BAR_P12", @"BAR-P12-01.hl7", @"BAR-P12-01-expected.json" },
                new[] { @"BAR_P12", @"BAR-P12-02.hl7", @"BAR-P12-02-expected.json" },

                new[] { @"DFT_P03", @"DFT-P03-01.hl7", @"DFT-P03-01-expected.json" },
                new[] { @"DFT_P03", @"DFT-P03-02.hl7", @"DFT-P03-02-expected.json" },
                new[] { @"DFT_P11", @"DFT-P11-01.hl7", @"DFT-P11-01-expected.json" },
                new[] { @"DFT_P11", @"DFT-P11-02.hl7", @"DFT-P11-02-expected.json" },

                new[] { @"OMG_O19", @"OMG-O19-01.hl7", @"OMG-O19-01-expected.json" },
                new[] { @"OMG_O19", @"OMG-O19-02.hl7", @"OMG-O19-02-expected.json" },

                new[] { @"REF_I12", @"REF-I12-01.hl7", @"REF-I12-01-expected.json" },
                new[] { @"REF_I12", @"REF-I12-02.hl7", @"REF-I12-02-expected.json" },
                new[] { @"REF_I14", @"REF-I14-01.hl7", @"REF-I14-01-expected.json" },
                new[] { @"REF_I14", @"REF-I14-02.hl7", @"REF-I14-02-expected.json" },

                new[] { @"ADT_A01", @"ADT01-23.hl7", @"ADT01-23-expected.json" },
                new[] { @"ADT_A01", @"ADT01-28.hl7", @"ADT01-28-expected.json" },
                new[] { @"ADT_A04", @"ADT04-23.hl7", @"ADT04-23-expected.json" },
                new[] { @"ADT_A04", @"ADT04-251.hl7", @"ADT04-251-expected.json" },
                new[] { @"ADT_A04", @"ADT04-28.hl7", @"ADT04-28-expected.json" },
                new[] { @"OML_O21", @"MDHHS-OML-O21-1.hl7", @"MDHHS-OML-O21-1-expected.json" },
                new[] { @"OML_O21", @"MDHHS-OML-O21-2.hl7", @"MDHHS-OML-O21-2-expected.json" },
                new[] { @"ORU_R01", @"LAB-ORU-1.hl7", @"LAB-ORU-1-expected.json" },
                new[] { @"ORU_R01", @"LAB-ORU-2.hl7", @"LAB-ORU-2-expected.json" },
                new[] { @"ORU_R01", @"LRI_2.0-NG_CBC_Typ_Message.hl7", @"LRI_2.0-NG_CBC_Typ_Message-expected.json" },
                new[] { @"ORU_R01", @"ORU-R01-RMGEAD.hl7", @"ORU-R01-RMGEAD-expected.json" },
                new[] { @"VXU_V04", @"IZ_1_1.1_Admin_Child_Max_Message.hl7", @"IZ_1_1.1_Admin_Child_Max_Message-expected.json" },
                new[] { @"VXU_V04", @"VXU.hl7", @"VXU-expected.json" },
            };
            return data.Select(item => new[]
            {
                item[0],
                Path.Join(Constants.SampleDataDirectory, "Hl7v2", item[1]),
                Path.Join(Constants.ExpectedDataFolder, "Hl7v2", item[0], item[2]),
            });
        }

        public static IEnumerable<object[]> GetDataForCcda()
        {
            var data = new List<string[]>
            {
                new[] { @"CCD", @"170.314B2_Amb_CCD.ccda", @"170.314B2_Amb_CCD-expected.json" },
                new[] { @"CCD", @"C-CDA_R2-1_CCD.xml.ccda", @"C-CDA_R2-1_CCD.xml-expected.json" },
                new[] { @"CCD", @"CCD.ccda", @"CCD-expected.json" },
                new[] { @"CCD", @"CCD-Parent-Document-Replace-C-CDAR2.1.ccda", @"CCD-Parent-Document-Replace-C-CDAR2.1-expected.json" },
                new[] { @"ConsultationNote", @"Care_Plan.ccda", @"Care_Plan-expected.json" },
                new[] { @"ConsultationNote", @"CDA_with_Embedded_PDF.ccda", @"CDA_with_Embedded_PDF-expected.json" },
                new[] { @"ConsultationNote", @"Consultation_Note.ccda", @"Consultation_Note-expected.json" },
                new[] { @"ConsultationNote", @"Unstructured_Document_embed.ccda", @"Unstructured_Document_embed-expected.json" },
                new[] { @"DischargeSummary", @"Discharge_Summary.ccda", @"Discharge_Summary-expected.json" },
                new[] { @"DischargeSummary", @"Consult-Document-Closing-Referral-C-CDAR2.1.ccda", @"Consult-Document-Closing-Referral-C-CDAR2.1-expected.json" },
                new[] { @"HistoryandPhysical", @"History_and_Physical.ccda", @"History_and_Physical-expected.json" },
                new[] { @"HistoryandPhysical", @"Diagnostic_Imaging_Report.ccda", @"Diagnostic_Imaging_Report-expected.json" },
                new[] { @"OperativeNote", @"Operative_Note.ccda", @"Operative_Note-expected.json" },
                new[] { @"OperativeNote", @"Patient-1.ccda", @"Patient-1-expected.json" },
                new[] { @"ProcedureNote", @"Procedure_Note.ccda", @"Procedure_Note-expected.json" },
                new[] { @"ProcedureNote", @"Patient-and-Provider-Organization-Direct-Address-C-CDAR2.1.ccda", @"Patient-and-Provider-Organization-Direct-Address-C-CDAR2.1-expected.json" },
                new[] { @"ProgressNote", @"Progress_Note.ccda", @"Progress_Note-expected.json" },
                new[] { @"ProgressNote", @"PROBLEMS_in_Empty_C-CDA_2.1-C-CDAR2.1.ccda", @"PROBLEMS_in_Empty_C-CDA_2.1-C-CDAR2.1-expected.json" },
                new[] { @"ReferralNote", @"Referral_Note.ccda", @"Referral_Note-expected.json" },
                new[] { @"ReferralNote", @"sample.ccda", @"sample-expected.json" },
                new[] { @"TransferSummary", @"Transfer_Summary.ccda", @"Transfer_Summary-expected.json" },
                new[] { @"TransferSummary", @"Unstructured_Document_reference.ccda", @"Unstructured_Document_reference-expected.json" },
            };
            return data.Select(item => new[]
            {
                item[0],
                Path.Join(Constants.SampleDataDirectory, "Ccda", item[1]),
                Path.Join(Constants.ExpectedDataFolder, "Ccda", item[0], item[2]),
            });
        }

        public static IEnumerable<object[]> GetDataForEcr()
        {
            var data = new List<string[]>
            {
                // Array has the following fields:
                // [
                //   1. Root template, 
                //   2. ecr file, 
                //   3. expected fhir file, 
                //   4. whether the file should fail at parsing or validation when testing if valid (if it is fully valid, "validation" is what should be there)
                //   5. The number of expected failures at the step in (4)
                // ]
                new[] { @"EICR", @"eCR_full.xml", @"eCR_full-expected.json", "validation", "13" },
                new[] { @"EICR", @"eCR_RR_combined_3_1.xml", @"eCR_RR_combined_3_1-expected.json", "parsing", "3" },
                new[] { @"EICR", @"eCR_EveEverywoman.xml", @"eCR_EveEverywoman-expected.json", "parsing", "19" },
            };
            return data.Select(item => new[]
            {
                item[0],
                Path.Join(Constants.SampleDataDirectory, "eCR", item[1]),
                Path.Join(Constants.ExpectedDataFolder, "eCR", item[0], item[2]),
                item[3],
                item[4],
            });
        }

        public static IEnumerable<object[]> GetDataForJson()
        {
            var data = new List<string[]>
            {
                new[] { @"ExamplePatient", @"ExamplePatient.json", @"ExamplePatient-expected.json" },
                new[] { @"Stu3ChargeItem", @"Stu3ChargeItem.json", @"Stu3ChargeItem-expected.json" },
            };
            return data.Select(item => new[]
            {
                item[0],
                Path.Join(Constants.SampleDataDirectory, "Json", item[1]),
                Path.Join(Constants.ExpectedDataFolder, "Json", item[0], item[2]),
            });
        }

        public static IEnumerable<object[]> GetDataForFhirToHl7v2()
        {
            var data = new List<string[]>
            {
                new[] { @"ObservationBundle", @"ObservationBundle.json", @"ObservationBundle-expected.hl7" },
            };
            return data.Select(item => new[]
            {
                item[0],
                Path.Join(Constants.SampleDataDirectory, "FHIR", item[1]),
                Path.Join(Constants.ExpectedDataFolder, "FhirToHl7v2", item[0], item[2]),
            });
        }

        public static IEnumerable<object[]> GetDataForStu3ToR4()
        {
            var data = new List<string>
            {
                // Maturity Level in R4 : N
                "CapabilityStatement",
                "CodeSystem",
                "Observation",
                "OperationDefinition",
                "OperationOutcome",
                "Parameters",
                "Patient",
                "StructureDefinition",
                "ValueSet",

                // Maturity Level in R4 : 3 & 4
                "SearchParameter",
                "ConceptMap",
                "Provenance",
                "AuditEvent",
                "DocumentReference",
                "MessageHeader",
                "Subscription",
                "Practitioner",
                "Organization",
                "Location",
                "Appointment",
                "AppointmentResponse",
                "Schedule",
                "Slot",
                "AllergyIntolerance",
                "Condition",
                "Procedure",
                "DiagnosticReport",
                "ImagingStudy",
                "QuestionnaireResponse",
                "MedicationRequest",
                "MedicationStatement",
                "Medication",
                "Immunization",
                "Questionnaire",

                // Maturity Level in R4 : 1 & 2
                "DetectedIssue",
            };

            return data.Select(item => new[]
            {
                item,
                Path.Join(Constants.SampleDataDirectory, "Stu3", item + ".json"),
                Path.Join(Constants.ExpectedDataFolder, "Stu3ToR4", item + ".json"),
            });
        }

        protected void ConvertHl7v2MessageAndValidateExpectedResponse(ITemplateProvider templateProvider, string rootTemplate, string inputFile, string expectedFile)
        {
            var hl7v2Processor = new Hl7v2Processor(_processorSettings, FhirConverterLogging.CreateLogger<Hl7v2Processor>());
            var inputContent = File.ReadAllText(inputFile);
            var expectedContent = File.ReadAllText(expectedFile);
            var traceInfo = new Hl7v2TraceInfo();
            var actualContent = hl7v2Processor.Convert(inputContent, rootTemplate, templateProvider, traceInfo);

            JsonSerializer serializer = new JsonSerializer();
            var expectedObject = serializer.Deserialize<JObject>(new JsonTextReader(new StringReader(expectedContent)));
            var actualObject = serializer.Deserialize<JObject>(new JsonTextReader(new StringReader(actualContent)));

            new List<string>
            {
                "$.entry[?(@.resource.resourceType=='Provenance')].resource.text.div",
            }.ForEach(path =>
            {
                expectedObject.SelectToken(path).Parent.Remove();
                actualObject.SelectToken(path).Parent.Remove();
            });

            Assert.True(JToken.DeepEquals(expectedObject, actualObject));
            Assert.True(traceInfo.UnusedSegments.Count > 0);
        }

        protected void ConvertCCDAMessageAndValidateExpectedResponse(ITemplateProvider templateProvider, string rootTemplate, string inputFile, string expectedFile)
        {
            var ccdaProcessor = new CcdaProcessor(_processorSettings, FhirConverterLogging.CreateLogger<CcdaProcessor>());
            var inputContent = File.ReadAllText(inputFile);
            var actualContent = ccdaProcessor.Convert(inputContent, rootTemplate, templateProvider);

            var updateSnaphot = Environment.GetEnvironmentVariable("UPDATE_SNAPSHOT") ?? "false";
            if (updateSnaphot.Trim() == "true")
            {
                File.WriteAllText(expectedFile, actualContent);
            }

            var expectedContent = File.ReadAllText(expectedFile);

            var expectedObject = JObject.Parse(expectedContent);
            var actualObject = JObject.Parse(actualContent);

            // Remove DocumentReference, where date is different every time conversion is run and gzip result is OS dependent
            if (expectedObject["entry"]?.Last()["resource"]["resourceType"].ToString() == "DocumentReference")
            {
                expectedObject["entry"]?.Last()?.Remove();
                actualObject["entry"]?.Last()?.Remove();
            }

            var diff = DiffHelper.FindDiff(actualObject, expectedObject);
            if (diff.HasValues)
            {
                Console.WriteLine(diff);
            }

            Assert.True(JToken.DeepEquals(expectedObject, actualObject));
        }

        protected void ValidateConvertCCDAMessageIsValidFHIR(ITemplateProvider templateProvider, string rootTemplate, string inputFile, string validationFailureStep, int numFailures)
        {
            var validateFhir = Environment.GetEnvironmentVariable("VALIDATE_FHIR") ?? "false";
            if (validateFhir.Trim() == "false") return;

            var ccdaProcessor = new CcdaProcessor(_processorSettings, FhirConverterLogging.CreateLogger<CcdaProcessor>());
            var inputContent = File.ReadAllText(inputFile);
            var actualContent = ccdaProcessor.Convert(inputContent, rootTemplate, templateProvider);

            var _jsonPocoDeserializer = new FhirJsonPocoDeserializer();

            try
            {
                var poco = _jsonPocoDeserializer.DeserializeResource(actualContent);
                var packageSource = new FhirPackageSource(
                    ModelInfo.ModelInspector,
                    "https://packages2.fhir.org/packages",
                    new string[] {
                        "hl7.fhir.us.ecr@2.1.2",
                    }
                );
                var profileSource = new CachedResolver(packageSource);
                var terminologyService = new LocalTerminologyService(profileSource);

                var validator = new Validator(profileSource, terminologyService);
                var result = validator.Validate(poco);
                var outcomeText = result.ToString();
                var numFailed = result.Issue.Count();

                if ("validation" == validationFailureStep && numFailures == numFailed)
                {
                    Console.WriteLine(result.ToString());
                }

                Assert.Equal("validation", validationFailureStep);
                Assert.True(numFailures == numFailed, $"!!Validation failed!!\nExpected {numFailures}, but got {numFailed}\n{outcomeText}");
            }
            catch (DeserializationFailedException e)
            {

                var errors = e.Message.Replace(") (", ")\n(");
                var numFailed = errors.Count(f => f == '\n') + 1;

                if ("parsing" == validationFailureStep && numFailures == numFailed)
                {
                    Console.WriteLine(errors);
                }

                Assert.Equal("parsing", validationFailureStep);
                Assert.True(numFailures == numFailed, $"!!Parsing failed!!\nExpected {numFailures}, but got {numFailed}\n{errors}");
                return;
            }
        }

        protected void ConvertFHIRMessageAndValidateExpectedResponse(ITemplateProvider templateProvider, string rootTemplate, string inputFile, string expectedFile)
        {
            var fhirProcessor = new FhirProcessor(_processorSettings, FhirConverterLogging.CreateLogger<FhirProcessor>());
            var inputContent = File.ReadAllText(inputFile);
            var expectedContent = File.ReadAllText(expectedFile);
            var actualContent = fhirProcessor.Convert(inputContent, rootTemplate, templateProvider);

            var expectedObject = JObject.Parse(expectedContent);
            var actualObject = JObject.Parse(actualContent);

            Assert.True(JToken.DeepEquals(expectedObject, actualObject));
        }

        protected void ConvertJsonMessageAndValidateExpectedResponse(ITemplateProvider templateProvider, string rootTemplate, string inputFile, string expectedFile)
        {
            var jsonProcessor = new JsonProcessor(_processorSettings, FhirConverterLogging.CreateLogger<JsonProcessor>());
            var inputContent = File.ReadAllText(inputFile);
            var expectedContent = File.ReadAllText(expectedFile);
            var actualContent = jsonProcessor.Convert(inputContent, rootTemplate, templateProvider);

            var expectedObject = JObject.Parse(expectedContent);
            var actualObject = JObject.Parse(actualContent);

            Assert.True(JToken.DeepEquals(expectedObject, actualObject));
        }

        protected void ConvertFhirBundleToHl7v2AndValidateExpectedResponse(ITemplateProvider templateProvider, string rootTemplate, string inputFile, string expectedFile)
        {
            var jsonProcessor = new FhirToHl7v2Processor(_processorSettings, FhirConverterLogging.CreateLogger<FhirToHl7v2Processor>());
            var inputContent = File.ReadAllText(inputFile);
            var expectedContent = File.ReadAllText(expectedFile);
            var actualContent = jsonProcessor.Convert(inputContent, rootTemplate, templateProvider);

            var fieldValues = actualContent.Split("|");

            // remove MSH-7 timestamp
            fieldValues[6] = string.Empty;
            var validatedContent = string.Join("|", fieldValues);

            Assert.Equal(expectedContent, validatedContent);
        }
    }
}
