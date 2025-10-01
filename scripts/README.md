# Scripts

## `loinc_names.py`

Updates the `src/Microsoft.Health.Fhir.Liquid.Converter/Loinc.csv` file. First download the Loinc database from here https://loinc.org/downloads/. Unzip file and locate the `LoincTable/Loinc.csv` file. Then run the script:
```
python loinc_names.py /path/to//Loinc.csv
```

Last ran on Loinc version 2.8.1

## `snomed_names.py`

Updates the `src/Microsoft.Health.Fhir.Liquid.Converter/Snomed.csv` file. Requires [pandas](https://pandas.pydata.org/) because working with multiple large CSVs is hard. If you do not have pandas in your global Python enviroment you can create a virtual enviroment using the `pyproject.toml` in this directory. I use [uv](https://docs.astral.sh/uv/):
```
uv syc
```

Once the Python environment is set up, download the Snomed database from here https://www.nlm.nih.gov/healthit/snomedct/us_edition.html. Unzip file, then run the script:
```
python loinc_names.py /path/to//Loinc.csv
```

Last ran on Snomed version 20250901
