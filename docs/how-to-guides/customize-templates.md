# Customize Liquid Templates
This how-to-guide shows you how to customize the liquid templates for the FHIR Converter service.

The default templates for the FHIR Converter service are located [here](https://github.com/CDCgov/dibbs-FHIR-Converter/tree/main/data/), and are a recommended starting point for creating customized templates.

The templates use the liquid template language, which is documented [here](https://shopify.github.io/liquid/).

## Transforming data to FHIR

### eCR to FHIR
The default templates are a good starting point for creating a custom eCR transformation template. A FHIR bundle is created by iterating over the common CCD sections in this [template](https://github.com/CDCgov/dibbs-FHIR-Converter/blob/main/data/Templates/eCR/EICR.liquid). As a starting point, you can start with the CCD sections that are needed and then modify the subtemplates referenced by this root template.

## Liquid Filters
Since the templates are written in the liquid templating language, there are a number of [liquid filters available](https://shopify.github.io/liquid/) which can assist with modifying string values. 

There are also some useful [filters and tags](https://github.com/CDCgov/dibbs-FHIR-Converter/blob/main/docs/Filters-and-Tags.md) definited in the FHIR-Converter project that can be used in templates as well.


