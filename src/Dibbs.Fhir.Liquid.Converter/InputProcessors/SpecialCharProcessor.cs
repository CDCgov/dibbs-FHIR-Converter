// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace Dibbs.Fhir.Liquid.Converter.InputProcessors
{
    public static class SpecialCharProcessor
    {
        public static string Escape(string input)
        {
            var evaluator = new MatchEvaluator(match => $"\\{match.Value}");
            return SpecialCharRegexes.EscapeRegex().Replace(input, evaluator);
        }

        public static string Unescape(string input)
        {
            var evaluator = new MatchEvaluator(match => match.Groups[1].Value);
            return SpecialCharRegexes.UnescapeRegex().Replace(input, evaluator);
        }
    }
}
