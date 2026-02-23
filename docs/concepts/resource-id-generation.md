# Resource ID generation

The default templates provided with the Converter computes Resource IDs using the input data fields. In order to preserve the generated Resource IDs, the default templates provide create **PUT requests**, instead of POST requests in the generated bundles.

For **eCR to FHIR conversion**, [ID generation template](data/Templates/eCR/Utils/GenerateId.liquid).

The Converter introduces a concept of "base resource/base ID". Base resources are independent entities, like Patient, Organization, Device, etc, whose IDs are defined as base ID. Base IDs could be used to generate IDs for other resources that relate to them. It helps enrich the input for hash and thus reduce ID collision.
For example, a Patient ID is used as part of hash input for an AllergyIntolerance ID, as this resource is closely related with a specific patient.

Below is an example where an AllergyIntolerance ID is generated, using ID/AllergyIntolerance template, AL1 segment and patient ID as its base ID.
The syntax is `{% evaluate [id] using [template] [variables] -%}`.

```liquid
{% evaluate allergyIntoleranceId using 'ID/AllergyIntolerance' AL1: al1Segment, baseId: patientId -%}
```