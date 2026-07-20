"""
Creates `src/Dibbs.Fhir.Liquid.Converter/Snomed.csv`

Parameters:
snomed_directory (str): Path to the unzipped directory downloaded from https://www.nlm.nih.gov/healthit/snomedct/us_edition.html
Returns:
None: Writes `src/Dibbs.Fhir.Liquid.Converter/Snomed.csv` from the given Snomed CSV.
"""

from argparse import ArgumentParser
from pathlib import Path
import pandas as pd


def find_release_file(snomed_directory, pattern):
    matches = sorted(Path(snomed_directory).glob(pattern))
    if len(matches) != 1:
        raise FileNotFoundError(
            f"Expected exactly one file matching {pattern}, found {len(matches)}"
        )
    return matches[0]


parser = ArgumentParser()
parser.add_argument("snomed_directory")
parser.add_argument(
    "--output-path",
    default="../src/Dibbs.Fhir.Liquid.Converter/Snomed.csv",
)
args = parser.parse_args()
snomed_directory = args.snomed_directory

description_file = find_release_file(
    snomed_directory,
    "**/Snapshot/Terminology/sct2_Description_Snapshot-en_US1000124_*.txt",
)
language_file = find_release_file(
    snomed_directory,
    "**/Snapshot/Refset/Language/der2_cRefset_LanguageSnapshot-en_US1000124_*.txt",
)

description_df = pd.read_csv(
    description_file,
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
    language_file,
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
    (merged["acceptabilityId"] == "900000000000548007")  # Preferred term
    & (merged["refsetId"] == "900000000000509007")       # US English
    & (merged["active_x"] == 1)                          # Active
].sort_values(by="conceptId")

deduped = filtered.loc[
    filtered.groupby("conceptId")["term"].apply(lambda x: x.str.len().idxmin())
]

deduped[["conceptId", "term"]].to_csv(args.output_path, index=False)
