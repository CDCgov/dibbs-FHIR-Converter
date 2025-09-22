"""
Creates `src/Microsoft.Health.Fhir.Liquid.Converter/Loinc.csv`

Will first check if a code has a `DisplayName`. Otherwise will use the code's `LONG_COMMON_NAME`

Parameters:
output_file (str): Path to LoincTable/Loinc.csv in the LOINC database downloaded from https://loinc.org

Returns:
None: Recreates `src/Microsoft.Health.Fhir.Liquid.Converter/Loinc.csv` from the given Loinc CSV.
"""
import csv
from argparse import ArgumentParser


argparser = ArgumentParser()
argparser.add_argument("loinc_csv_path")

args = argparser.parse_args()

LOINC_NUM_HEADER = "LOINC_NUM"
LONG_COMMON_NAME_HEADER = "LONG_COMMON_NAME"
DISPLAY_NAME_HEADER = "DisplayName"

with open(args.loinc_csv_path) as loinc_csv_file:
    csv_reader = csv.DictReader(loinc_csv_file)
    next(csv_reader)

    with open(
        "../src/Microsoft.Health.Fhir.Liquid.Converter/Loinc.csv", "w"
    ) as output_file:
        writer = csv.writer(output_file)
        writer.writerow([LOINC_NUM_HEADER, "name"])

        for line in csv_reader:
            if line[DISPLAY_NAME_HEADER]:
                name_column = DISPLAY_NAME_HEADER
            else:
                name_column = LONG_COMMON_NAME_HEADER
            writer.writerow(
                [
                    line[LOINC_NUM_HEADER],
                    line[name_column],
                ]
            )
