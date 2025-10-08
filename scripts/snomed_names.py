"""
Creates `src/Microsoft.Health.Fhir.Liquid.Converter/Snomed.csv`

Parameters:
snomed_directory (str): Path to the unzipped directory downloaded from https://www.nlm.nih.gov/healthit/snomedct/us_edition.html
Returns:
None: Writes `src/Microsoft.Health.Fhir.Liquid.Converter/Snomed.csv` from the given Snomed CSV.
"""

from argparse import ArgumentParser
import pandas as pd

parser = ArgumentParser()
parser.add_argument("snomed_directory")
snomed_directory = parser.parse_args().snomed_directory

description_df = pd.read_csv(
    snomed_directory + "/Snapshot/Terminology/sct2_Description_Snapshot-en_US1000124_20250901.txt",
    delimiter="\t",
    dtype={
        "id": "string",
        "effectiveTime": "string",
        "active": "boolean",
        "moduleId": "string",
        "conceptId": "string",
        "languageCode": "string",
        "typeId": "string",
        "term": "string",
        "caseSignificanceId": "string",
    },
)
language_df = pd.read_csv(
    snomed_directory + "/Snapshot/Refset/Language/der2_cRefset_LanguageSnapshot-en_US1000124_20250901.txt",
    delimiter="\t",
    dtype={
        "id": "string",
        "effectiveTime": "string",
        "active": "boolean",
        "moduleId": "string",
        "refsetId": "string",
        "referencedComponentId": "string",
        "acceptabilityId": "string",
    },
)

merged = pd.merge(
    description_df,
    language_df,
    left_on="id",
    right_on="referencedComponentId",
    how="left",
)

filtered = merged[
    (merged["acceptabilityId"] == "900000000000548007") # Preferred term
    & (merged["refsetId"] == "900000000000509007")      # US English
    & (merged["active_x"] == 1)                         # Active
].sort_values(by="conceptId")

deduped = filtered.loc[
    filtered.groupby("conceptId")["term"].apply(lambda x: x.str.len().idxmin())
]

all_concept_ids = set(description_df["conceptId"])
current_concept_ids = set(deduped["conceptId"])


deduped[["conceptId", "term"]].to_csv(
    "../src/Microsoft.Health.Fhir.Liquid.Converter/Snomed.csv", index=False
)
