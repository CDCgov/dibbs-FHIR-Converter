using System.Collections.Concurrent;
using System.Text.Json;
using Firely.Fhir.Packages;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Specification.Terminology;
using Hl7.Fhir.Utility;
using Task = System.Threading.Tasks.Task;

internal class ConformanceService
{
    private readonly ConcurrentDictionary<string, string> _oidUriMapping;
    private readonly ConcurrentDictionary<string, Dictionary<string, string>> _codeDisplayMapping;
    private readonly Dictionary<string, string> _loincDictionary;
    private readonly Dictionary<string, string> _snomedDictionary;
    private readonly Dictionary<string, string> _rxnormDictionary;
    private FhirPackageSource _resolver;
    private volatile bool _isInitialized;

    internal ConformanceService()
    {
        _oidUriMapping = new ConcurrentDictionary<string, string>();
        _codeDisplayMapping = new ConcurrentDictionary<string, Dictionary<string, string>>();
        _loincDictionary = [];
        _snomedDictionary = [];
        _rxnormDictionary = [];
        _resolver = null;
    }

    public bool IsInitialized => _isInitialized;

    internal async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        _resolver = new FhirPackageSource(
            ModelInfo.ModelInspector,
            "https://packages2.fhir.org/packages",
            ["hl7.fhir.us.ecr@2.1.2",]
        );

        await MakeCodeSystemOidMapping();
        await MakeNamingSystemOidMapping();
        await MakeCodeLibrary();
        MakeTerminologyCodeDisplayMappings();

        _isInitialized = true;
    }

    private void MakeTerminologyCodeDisplayMappings()
    {

        var outDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        File.ReadLines(Path.Combine(outDir, "Loinc.csv"))
            .Select(line => line.Split(','))
            .Where(parts => parts.Length >= 2)
            .ToList()
            .ForEach(parts => _loincDictionary[parts[0].Trim()] = parts[1].Trim());

        File.ReadLines(Path.Combine(outDir, "Snomed.csv"))
            .Select(line => line.Split(','))
            .Where(parts => parts.Length >= 2)
            .ToList()
            .ForEach(parts => _snomedDictionary[parts[0].Trim()] = parts[1].Trim());

        File.ReadLines(Path.Combine(outDir, "rxnorm.csv"))
            .Select(line => line.Split(','))
            .Where(parts => parts.Length >= 2)
            .ToList()
            .ForEach(parts => _rxnormDictionary[parts[0].Trim()] = parts[1].Trim());
    }

    private async Task MakeCodeLibrary()
    {
        var valueSets = _resolver.ListResourceUris(ResourceType.ValueSet.ToString());
        foreach (var valueSetUri in valueSets)
        {
            if (await _resolver.ResolveByUriAsync(valueSetUri) is not ValueSet valueSet)
            {
                continue;
            }
            else
            {
                var contains = valueSet.Expansion?.Contains ?? [];

                foreach (var code in contains)
                {
                    var system = code.System;
                    var codeCode = code.Code;
                    var display = code.Display;
                    if (system != null && codeCode != null && display != null)
                    {


                        if (_codeDisplayMapping.ContainsKey(system))
                        {
                            _codeDisplayMapping[system].TryAdd(codeCode, code.Display);
                        }
                        else
                        {
                            _codeDisplayMapping.TryAdd(
                                system,
                                new Dictionary<string, string>
                                {
                                {
                                    codeCode,
                                    code.Display
                                },
                                }
                            );
                        }
                    }

                }

                var includes = valueSet.Compose?.Include ?? [];

                foreach (var codeSystem in includes)
                {
                    var system = codeSystem.System;

                    foreach (var concept in codeSystem.Concept)
                    {
                        var code = concept.Code;
                        var display = concept.Display;

                        if (_codeDisplayMapping.TryGetValue(system, out Dictionary<string, string>? value))
                        {
                            value.TryAdd(code, display);
                        }
                        else
                        {
                            _codeDisplayMapping.TryAdd(
                                system,
                                new Dictionary<string, string>
                                {
                                    {
                                        code,
                                        display
                                    },
                                }
                            );
                        }
                    }
                }
            }
        }

        //         string json = JsonSerializer.Serialize(_codeDisplayMapping);
        // File.WriteAllText(@"/Users/jnygaard/dev/Skylight/dibbs/dibbs-FHIR-Converter/out.json", json);
    }

    private async Task MakeCodeSystemOidMapping()
    {
        var codeSystems = _resolver.ListResourceUris(ResourceType.CodeSystem.ToString());
        foreach (var codeSystemUri in codeSystems)
        {
            if (await _resolver.ResolveByUriAsync(codeSystemUri) is not CodeSystem codeSystem)
            {
                continue;
            }
            else
            {
                if (codeSystem.Identifier.IsNullOrEmpty() || codeSystem.Status == PublicationStatus.Retired)
                {
                    continue;
                }

                var oid = codeSystem
                    .Identifier.Find(item => item.Use != Identifier.IdentifierUse.Old)
                    ?.Value;

                if (oid == null)
                {
                    continue;
                }

                if (oid.StartsWith("urn:oid:"))
                {
                    oid = oid[8..];
                }

                if (_oidUriMapping.ContainsKey(oid))
                {
                    continue;
                }

                _oidUriMapping.TryAdd(oid, codeSystem.Url);
            }
        }
    }

    private async Task MakeNamingSystemOidMapping()
    {
        var namingSystems = _resolver.ListResourceUris(ResourceType.NamingSystem.ToString());
        foreach (var namingSystemUri in namingSystems)
        {
            if (await _resolver.ResolveByUriAsync(namingSystemUri) is not NamingSystem namingSystem)
            {
                continue;
            }
            else
            {
                if (namingSystem.UniqueId.IsNullOrEmpty())
                {
                    continue;
                }

                var oid = namingSystem
                    .UniqueId.Find(item => item.Type == NamingSystem.NamingSystemIdentifierType.Oid)
                    ?.Value;
                if (oid == null)
                {
                    continue;
                }

                if (oid.StartsWith("urn:oid"))
                {
                    oid = oid[8..];
                }

                if (_oidUriMapping.ContainsKey(oid))
                {
                    continue;
                }

                var uri =
                    namingSystem
                        .UniqueId.Find(item =>
                            item.Type == NamingSystem.NamingSystemIdentifierType.Uri && item.Preferred == true
                        )
                        ?.Value
                    ?? namingSystem
                        .Extension.Find(item =>
                            item.Url
                            == "http://hl7.org/fhir/5.0/StructureDefinition/extension-NamingSystem.url")
                        ?.Value.ToString();

                _oidUriMapping.TryAdd(oid, uri);
            }
        }
    }

    internal string? GetCodeSystemUri(string? oid)
    {
        oid = oid?.Trim() ?? string.Empty;

        if (_oidUriMapping.TryGetValue(oid, out string? uri))
        {
            return uri;
        }
        else
        {
            if (!oid.StartsWith("urn:oid:"))
            {
                return "urn:oid:" + oid;
            }
            else
            {
                return oid;
            }
        }
    }

    internal string? GetCodeDisplay(string system, string code)
    {
        string? display = null;

        _codeDisplayMapping.TryGetValue(system, out Dictionary<string, string> codes);
        codes?.TryGetValue(code, out display);

        if (display == null)
        {
            if (system == "http://loinc.org")
            {
                _loincDictionary.TryGetValue(code, out display);
            }
            else if (system == "http://snomed.info/sct")
            {
                _snomedDictionary.TryGetValue(code, out display);
            }
            else if (system.Contains("rxnorm"))
            {
                _rxnormDictionary.TryGetValue(code, out display);
            }
        }

        return display;
    }
}
