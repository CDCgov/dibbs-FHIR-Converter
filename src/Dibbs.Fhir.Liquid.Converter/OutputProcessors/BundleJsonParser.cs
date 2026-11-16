// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using Dibbs.Fhir.Liquid.Converter.Exceptions;
using Dibbs.Fhir.Liquid.Converter.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dibbs.Fhir.Liquid.Converter.OutputProcessors
{
    // Only the root object and its entry array are read incrementally. ANTLR and
    // JsonListener still validate and clean each value using the existing grammar.
    internal sealed class BundleJsonParser
    {
        private const int OpenObject = jsonParser.T__0;
        private const int Comma = jsonParser.T__1;
        private const int CloseObject = jsonParser.T__2;
        private const int Colon = jsonParser.T__3;
        private const int OpenArray = jsonParser.T__4;
        private const int CloseArray = jsonParser.T__5;

        private readonly UnbufferedTokenStream tokens;
        private readonly jsonParser parser;

        public BundleJsonParser(string input)
        {
            var lexer = new jsonLexer(new AntlrInputStream(input));
            lexer.RemoveErrorListeners();
            tokens = new UnbufferedTokenStream(lexer);
            parser = new jsonParser(tokens)
            {
                BuildParseTree = true,
                ErrorHandler = new FailFastErrorStrategy(),
            };
            parser.RemoveErrorListeners();
        }

        public JObject Parse()
        {
            try
            {
                var result = new JObject();
                Expect(OpenObject);
                SkipCommas();
                while (tokens.LA(1) != CloseObject)
                {
                    var propertyName = JsonConvert.DeserializeObject<string>(Expect(jsonParser.STRING).Text);
                    Expect(Colon);
                    var value = propertyName == "entry" && tokens.LA(1) == OpenArray
                        ? ReadEntries()
                        : ReadValue(parentDepth: 1);
                    if (value != null)
                    {
                        // Empty values are removed before duplicate properties are resolved.
                        // JsonConvert places a repeated property at its last occurrence.
                        result.Remove(propertyName);
                        result.Add(propertyName, value);
                    }

                    ReadSeparator(CloseObject);
                }

                Expect(CloseObject);
                return result;
            }
            catch (ParseCanceledException)
            {
                throw UnexpectedToken();
            }
        }

        private JToken ReadEntries()
        {
            var entries = new JArray();
            Expect(OpenArray);
            SkipCommas();
            while (tokens.LA(1) != CloseArray)
            {
                var value = ReadValue(parentDepth: 2);
                if (value != null)
                {
                    entries.Add(value);
                }

                ReadSeparator(CloseArray);
            }

            Expect(CloseArray);
            return entries.Count == 0 ? null : entries;
        }

        private JToken ReadValue(int parentDepth)
        {
            // Neither the token stream nor the parser retains earlier entry trees.
            var tree = parser.value();
            var listener = new JsonListener();
            ParseTreeWalker.Default.Walk(listener, tree);
            var cleaned = listener.GetResult() as string;
            if (cleaned == null)
            {
                return null;
            }

            using var reader = new JsonTextReader(new StringReader(cleaned))
            {
                DateParseHandling = DateParseHandling.None,

                // Preserve Newtonsoft's default document depth limit when reading a fragment.
                MaxDepth = 64 - parentDepth,
            };

            // Match JsonConvert's duplicate-property ordering within each value too.
            using var writer = new JTokenWriter();
            reader.Read();
            writer.WriteToken(reader);
            return writer.Token;
        }

        private IToken Expect(int type)
        {
            var token = tokens.LT(1);
            if (token.Type != type)
            {
                throw UnexpectedToken();
            }

            tokens.Consume();
            return token;
        }

        private void ReadSeparator(int closingToken)
        {
            if (tokens.LA(1) == closingToken)
            {
                return;
            }

            // Extra commas are allowed by json.g4; missing commas are not.
            Expect(Comma);
            SkipCommas();
        }

        private void SkipCommas()
        {
            while (tokens.LA(1) == Comma)
            {
                tokens.Consume();
            }
        }

        private PostprocessException UnexpectedToken()
        {
            var token = tokens.LT(1);
            return ParsingError($"Unexpected token at line {token.Line}:{token.Column}.");
        }

        private static PostprocessException ParsingError(string message)
        {
            return new PostprocessException(
                FhirConverterErrorCode.JsonParsingError,
                string.Format(Resources.JsonParsingError, message));
        }

        private sealed class FailFastErrorStrategy : BailErrorStrategy
        {
            public override void ReportError(Parser recognizer, RecognitionException e)
            {
                // Default error recovery can request tokens already discarded by the stream.
                throw new ParseCanceledException(e);
            }
        }
    }
}
