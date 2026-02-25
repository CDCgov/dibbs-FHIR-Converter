using System.Text.RegularExpressions;

namespace Dibbs.Fhir.Liquid.Converter
{
    /// <summary>
    /// Contains generated regex needed for DictionaryJsonConverter.
    /// </summary>
    public partial class DictionaryJsonConverterRegex
    {
        [GeneratedRegex(@"\r\n?|\n")]
        public static partial Regex NewlineRegex();
    }
}