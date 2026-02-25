using System.Text.RegularExpressions;

namespace Dibbs.Fhir.Liquid.Converter.InputProcessors
{
    /// <summary>
    /// Contains generated regexes needed for SpecialCharProcessor.
    /// </summary>
    public static partial class SpecialCharRegexes
    {
        [GeneratedRegex(@"\\|""")]
        public static partial Regex EscapeRegex();

        [GeneratedRegex(@"\\(\\|"")")]
        public static partial Regex UnescapeRegex();
    }
}