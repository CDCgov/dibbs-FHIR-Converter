using System.Text.RegularExpressions;

namespace Dibbs.Fhir.Liquid.Converter.DataParsers
{
    /// <summary>
    /// Contains generated regex needed for CcdaDataParser.
    /// </summary>
    public static partial class CcdaDataParserRegex
    {
        [GeneratedRegex(@"\r\n?|\n")]
        public static partial Regex NewlineRegex();
    }
}