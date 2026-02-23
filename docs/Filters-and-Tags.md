# Filters and Tags

## Filters

By default, Liquid provides a set of [standard filters](https://github.com/Shopify/liquid/wiki/Liquid-for-Designers#standard-filters) to assist template creation.
Besides these filters, FHIR Converter also provides some other filters that are useful in conversion, which are listed below.
If these filters do not meet your needs, you can also write your own filters.

### Section filters
| Filter | Description | Syntax |
|-|-|-|
| get_first_ccda_sections_by_template_id | Returns first instance (non-alphanumeric chars replace by '_' in name) of the sections by template id | `{% assign firstSections = msg \| get_first_ccda_sections_by_template_id: '2.16.840.1.113883.10.20.22.2.5.1' -%}` |

### String Filters
| Filter | Description | Syntax |
|-|-|-|
| escape_special_chars | Returns string with special chars escaped | `{{ '\E' \| escape_special_chars }} #=> '\\E'` |
| match | Returns an array containing matches with a regular expression | `{% assign m = code \| match: '[0123456789.]+' -%}` |
| append | Adds the first argument to the end of the input string. Returns nil if the first argument is nil. | `{% assign fullPatientId = 'Patient/' \| append: patientId -%}` |
| prepend | Adds the input string to the end of the first argument. Returns nil if the first argument is nil. | `{% assign fullPatientId = patientId \| prepend: 'Patient/' -%}` |
| to_json_string | Converts to JSON string | `{% assign msgJsonString = msg \| to_json_string -%}` |
| gzip | Returns compressed string | `{{ uncompressedData \| gzip }}` |
| to_xhtml | Formats input XHTML string as an XHTML document with one root `div` element | `{{ component.section.text._innerText \| to_xhtml }}` |
| remove_regex | Removes string matching regex argument from input string | `{{ContactPoint.value \| remove_regex: 'tel:\\s*'}}` |

### DateTime filters
| Filter | Description | Syntax |
|-|-|-|
| add_hyphens_date | Adds hyphens to a date or a partial date that does not have hyphens to make it into a valid FHIR format. The input date format is YYYY, YYYYMM, or YYYYMMDD. The output format is a valid FHIR date or a partial date format: YYYY, YYYY-MM, or YYYY-MM-DD.  | `{{ PID.7.Value \| add_hyphens_date }}` |
| format_as_date_time | Converts valid HL7v2 and C-CDA datetime to a valid FHIR datetime format. The input datetime format is datetime or partial datetime without hyphens: YYYY[MM[DD[HH[MM[SS[.S[S[S[S]]]]]]]]][+/-ZZZZ]. For example, the input 20040629175400000 will have the output 2004-06-29T17:54:00.000Z. Provides parameters to handle different time zones: preserve, utc, local. The default method is preserve. | `{{ PID.29.Value \| format_as_date_time: 'utc' }}` |
| now | Provides the current time in a specific format. The default format is yyyy-MM-ddTHH:mm:ss.FFFZ. | `{{ '' \| now: 'dddd, dd MMMM yyyy HH:mm:ss' }}` |
| format_width_as_period | Creates a low and high value from either a low or high value plus a width. | `{{ effectiveTime \| format_width_as_period }}` |

DateTime filters usage examples:

- add_hyphens_date
```
    {{ "2001" | add_hyphens_date }} -> 2001
    {{ "200101" | add_hyphens_date }} -> 2001-01
    {{ "20010101" | add_hyphens_date }} -> 2001-01-01
```

- format_as_date_time
```
    {{ "20110103143428-0800" | format_as_date_time }} -> 2011-01-03T14:34:28-08:00
    {{ "20110103143428-0800" | format_as_date_time: 'preserve' }} -> 2011-01-03T14:34:28-08:00
    {{ "20110103143428-0800" | format_as_date_time: 'utc' }} -> 2011-01-03T22:34:28Z
```
>[Note] : `format_as_date_time` and `add_hyphens_date` are used to convert HL7v2 and C-CDA date and datetime format to FHIR. For other date or datetime's reformat, please refer to the standard filter [date](https://shopify.dev/api/liquid/filters/additional-filters#date).
- now
```
    {{ "" | now  }} -> 2022-03-22T06:50:25.071Z // an example time
    {{ "" | now: 'dddd, dd MMMM yyyy HH:mm:ss' }} -> Tuesday, 22 March 2022 06:52:15
    {{ "" | now: 'd' }} -> 3/22/2022
```

> [Note] : If the input is a partial datetime without time zone, it will be set to the first day of the year and 00:00:00 clock time with local time zone as suffix. e.g. In the location with +08:00 time zone, the input string "201101031434" will be filled to "20110103143400+0800". The template {{ "201101031434" | format_as_date_time: 'utc'}} will output 2011-01-03T06:34:00Z when running at +08:00 location.


### Collection filters
| Filter | Description | Syntax |
|-|-|-|
| to_array | Returns an array created (if needed) from given object | `{% assign authors = msg.ClinicalDocument.author \| to_array -%}` |
| batch_render | Render every entry in a collection with a snippet and a variable name set in snippet | `{{ firstSections.2_16_840_1_113883_10_20_22_2_5_1.entry \| to_array \| batch_render: 'Entry/Problem/entry', 'entry' }}` |
| nested_where | Given a collection, return items that match the keypath and target property (like standard "where" filter except instead of taking one key, takes a period-delimited path of keys). | `{{ obs \| nested_where: 'templateId.root', '2.16.840.1.113883.10.20.22.4.421' }}` |

### Miscellaneous filters
| Filter | Description | Syntax |
|-|-|-|
| get_property | Returns a specific property of a coding with mapping file [CodeSystem.json](../data/Templates/Hl7v2/CodeSystem/CodeSystem.json) | `{{ PID.8.Value \| get_property: 'CodeSystem/Gender', 'code' }}` |
| generate_uuid | Generates an ID based on an input string | `{% assign patientId = firstSegments.PID.3.1.Value \| generate_uuid -%}` |
| generate_id_input | Generates an input string for generate_uuid with 1) the resource type, 2) whether a base ID is required, 3) the base ID (optional) | `{{ identifiers \| generate_id_input: 'Observation', false, baseId \| generate_uuid }}` |
| clean_string_from_tabs | Replaces tabs in input string with spaces | `{{ 'space\tbetween' \| clean_string_from_tabs }}` |
| print_object | Prints JSON representation of input to console. (**Note:** `DEBUG_LOG` environment variable must be set to `true`) | `{{ object_to_print \| print_object }}` |
| get_loinc_name | Retrieves the name associated with the specified LOINC code from the LOINC dictionary | `{%- assign display = Coding.code \| get_loinc_name -%}` |
| get_rxnorm_name | Retrieves the name associated with the specified RxNorm code from the RxNorm dictionary | `{%- assign display = Coding.code \| get_rxnorm_name -%}` |
| get_snomed_name | Retrieves the name associated with the specified Snomed code from the Snomed dictionary | `{%- assign display = Coding.code \| get_snomed_name -%}` |
| find_inner_text_by_id | Searches for the original text content of a node with a specified ID within a given xml string (`text._innerText` in most cases). | `{% assign commentStr = text \| find_inner_text_by_id: refVal -%}` |
| format_quantity | Formats quantity into valid json number | `{{ medicationAdministration.doseQuantity.value \| format_quantity }}` |


## Tags

By default, Liquid provides a set of standard [tags](https://github.com/Shopify/liquid/wiki/Liquid-for-Designers#tags) to assist in template creation. Besides these tags, FHIR Converter also provides some other tags useful in conversion, which are listed below. If these tags do not meet your needs, you can write your own tags.

| Tag | Description | Syntax |
|-|-|-|
| evaluate | Evaluates an ID with an ID generation template and input data | `{% evaluate patientId using 'Utils/GenerateId' obj:msg.ClinicalDocument.recordTarget.patientRole -%}` |