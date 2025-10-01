from argparse import ArgumentParser
import csv
import json
from pathlib import Path


parser = ArgumentParser()
parser.add_argument("path")

args = parser.parse_args()
path = args.path

base_path = Path(path)

with open(
        "../src/Microsoft.Health.Fhir.Liquid.Converter/CodeSystems.csv", "w"
    ) as output_file:
    writer = csv.writer(output_file)
    writer.writerow(["oid", "url"])

    for entry in base_path.rglob('*.json'):
        if entry.is_file():
            with open(entry, 'r') as file:
                data = json.load(file)
                oid = None
                if data.get("resourceType") == "NamingSystem" and data.get("name") == "LOINC":
                    continue

                if data.get("identifier") and any(x for x in data["identifier"] if x.get("type") == "oid" or x.get("system") == "urn:ietf:rfc:3986"):
                    oid = [x["value"] for x in data.get("identifier") if x.get("type") == "oid" or x.get("system") == "urn:ietf:rfc:3986"][0].removeprefix("urn:oid:")
                elif data.get("uniqueId") and any(x for x in data["uniqueId"] if x.get("type") == "oid"):
                    oid = [x["value"] for x in data.get("uniqueId") if x.get("type") == "oid"][0].removeprefix("urn:oid:")

                if not oid:
                    continue

                if data.get("identifier") and any(x for x in data["identifier"] if x.get("type") == "uri"):
                    url = [x["value"] for x in data.get("identifier") if x.get("type") == "uri"][0]
                if data.get("uniqueId") and any(x for x in data["uniqueId"] if x.get("type") == "uri"):
                    url = [x["value"] for x in data.get("uniqueId") if x.get("type") == "uri"][0]
                elif data.get("url"):
                    url = data["url"]
                elif data.get("extension") and [x for x in data["extension"] if x["url"] == "http://hl7.org/fhir/5.0/StructureDefinition/extension-NamingSystem.url"]:
                    url = [x for x in data["extension"] if x["url"] == "http://hl7.org/fhir/5.0/StructureDefinition/extension-NamingSystem.url"][0]["valueUri"]
                else:
                    url = "COULDN'T FIND"

                writer.writerow([oid, url])

