using System.Threading.Tasks;
using Firely.Fhir.Packages;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Specification.Terminology;
using Hl7.Fhir.Utility;
using Newtonsoft.Json;

namespace Dibbs.FhirResolverTests;

public class UnitTest1
{
    [Fact]
    public async System.Threading.Tasks.Task GetCodeSystemUri()
    {
        await ConformanceManager.InitializeAsync();
        var result = ConformanceManager.GetCodeSystemUri("2.16.840.1.113883.6.1");
        Assert.Equal("http://loinc.org", result);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetCodeDisplay()
    {
        await ConformanceManager.InitializeAsync();

        var system = "http://snomed.info/sct";
        var code = "407377005";
        var result = ConformanceManager.GetCodeDisplay(system, code);
        Assert.Equal("Female-to-male transsexual (finding)", result);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetCodeDisplay_EICR()
    {
        await ConformanceManager.InitializeAsync();

        var system = "http://loinc.org";
        var code = "55751-2";
        var result = ConformanceManager.GetCodeDisplay(system, code);
        Assert.Equal("Public health case report Document", result);
    }
}
