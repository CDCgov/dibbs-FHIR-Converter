public static class ConformanceManager
{
    private static readonly Lazy<ConformanceService> _instance =
        new (static () => new ConformanceService());

    private static ConformanceService Instance => _instance.Value;

    public static bool IsInitialized => Instance.IsInitialized;

    // Public static methods that delegate to the singleton
    public static async Task InitializeAsync()
    {
        await Instance.InitializeAsync();
    }

    public static string? GetCodeSystemUri(string? oid)
    {
        return Instance.GetCodeSystemUri(oid);
    }

    public static string? GetCodeDisplay(string system, string code)
    {
        return Instance.GetCodeDisplay(system, code);
    }
}
