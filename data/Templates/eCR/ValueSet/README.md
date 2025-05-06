# ValueSet Sources

### ValueSet: DiagnosticReportStatus
eICR `Laboratory Result Status` -> FHIR `DiagnosticReport` Resource status
- **eICR Version 3.1**
  - **URL:** https://terminology.hl7.org/ValueSet-v2-0123.html
  - **Notes:** eICR V3.1 spec adds a Laboratory Result Status observation that uses this ValueSet. The template OID is `2.16.840.1.113883.10.20.22.4.418`
- **eICR Version 1.1: HL7 ActStatus**
  - **URL:** https://terminology.hl7.org/2.0.0/CodeSystem-v3-ActStatus.html
  - **Notes:** Status used as a fallback if the Laboratory Result Status observation does not exist.
- **FHIR ValueSet: `DiagnosticReport` status**
  - **URL:** https://hl7.org/fhir/R4/valueset-diagnostic-report-status.html



### ValueSet: ObservationResultStatus
eICR `Laboratory Observation Result Status` -> FHIR (Lab) `Observation` Resource status
- **eICR Version 3.1**
  - **URL:** https://build.fhir.org/ig/HL7/UTG/ValueSet-v2-0085.html
  - **Notes:** eICR V3.1 spec adds a Laboratory Observation Result Status observation that uses this ValueSet. The template OID is `2.16.840.1.113883.10.20.22.4.419`
- **eICR Version 1.1: HL7 ActStatus**
  - **URL:** https://terminology.hl7.org/2.0.0/CodeSystem-v3-ActStatus.html
  - **Notes:** Status used as a fallback if the Laboratory Observation Result Status observation does not exist.
- **FHIR ValueSet: `Observation` status**
  - **URL:** https://build.fhir.org/valueset-observation-status.html
