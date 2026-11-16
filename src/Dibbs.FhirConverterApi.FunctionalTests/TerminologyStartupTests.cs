using System.Diagnostics;

namespace Dibbs.FhirConverterApi.FunctionalTests;

public class TerminologyStartupTests
{
    [Theory]
    [InlineData("Loinc.csv", null)]
    [InlineData("Snomed.csv", null)]
    [InlineData("rxnorm.csv", null)]
    [InlineData("Loinc.csv", "")]
    [InlineData("Snomed.csv", "")]
    [InlineData("rxnorm.csv", "")]
    [InlineData("Loinc.csv", "code-without-description\n")]
    [InlineData("Snomed.csv", "code-without-description\n")]
    [InlineData("rxnorm.csv", "code-without-description\n")]
    public async Task Startup_ExitsUnsuccessfully_WhenTerminologyIsInvalid(string filename, string? contents)
    {
        // Each case needs a fresh process because terminology initialization is static.
        var directory = Directory.CreateTempSubdirectory("fhir-terminology-startup-");
        try
        {
            foreach (var file in Directory.EnumerateFiles(AppContext.BaseDirectory))
            {
                if (Path.GetExtension(file) is ".dll" or ".json")
                {
                    File.Copy(file, Path.Combine(directory.FullName, Path.GetFileName(file)));
                }
            }

            foreach (var terminologyFile in new[] { "Loinc.csv", "Snomed.csv", "rxnorm.csv" })
            {
                await File.WriteAllTextAsync(Path.Combine(directory.FullName, terminologyFile), "code,description\n");
            }

            var path = Path.Combine(directory.FullName, filename);
            if (contents is null)
            {
                File.Delete(path);
            }
            else
            {
                await File.WriteAllTextAsync(path, contents);
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet",
                WorkingDirectory = directory.FullName,
                UseShellExecute = false,
                ArgumentList = { "Dibbs.FhirConverterApi.dll", "--urls", "http://127.0.0.1:0" },
            };
            startInfo.Environment["TEMPLATES_PATH"] = Path.GetFullPath(
                Environment.GetEnvironmentVariable("TEMPLATES_PATH") ?? "../../../../../data/Templates/eCR");
            startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
            startInfo.Environment["DOTNET_ENVIRONMENT"] = "Production";

            using var process = new Process { StartInfo = startInfo };
            Assert.True(process.Start(), "Could not start the API process.");
            try
            {
                // Hang guard: a regression that starts the server must not hang the suite.
                await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(60));
                Assert.NotEqual(0, process.ExitCode);
            }
            finally
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
                }
            }
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }
}
