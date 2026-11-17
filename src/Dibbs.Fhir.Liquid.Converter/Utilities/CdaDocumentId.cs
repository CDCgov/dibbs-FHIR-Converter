// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Dibbs.Fhir.Liquid.Converter.Utilities
{
    internal static class CdaDocumentId
    {
        // Match generate_uuid over ToString(DisableFormatting) with CR/LF removed,
        // without allocating the entire serialized XML or its UTF-8 byte array.
        public static string Generate(XDocument document)
        {
            using var hash = SHA256.Create();
            using (var stream = new CryptoStream(Stream.Null, hash, CryptoStreamMode.Write))
            using (var utf8 = new StreamWriter(stream, new UTF8Encoding(false)))
            using (var text = new NewlineRemovingWriter(utf8))
            using (var xml = XmlWriter.Create(text, new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                ConformanceLevel = ConformanceLevel.Auto,
            }))
            {
                document.WriteTo(xml);
            }

            return new Guid(hash.Hash.AsSpan(0, 16)).ToString();
        }

        private sealed class NewlineRemovingWriter : TextWriter
        {
            private readonly TextWriter output;

            public NewlineRemovingWriter(TextWriter output)
            {
                this.output = output;
            }

            public override Encoding Encoding => output.Encoding;

            public override void Write(char value)
            {
                if (value is not '\r' and not '\n')
                {
                    output.Write(value);
                }
            }

            public override void Write(string value) => Write(value.AsSpan());

            public override void Write(char[] buffer, int index, int count) => Write(buffer.AsSpan(index, count));

            public override void Write(ReadOnlySpan<char> value)
            {
                int newline;
                while ((newline = value.IndexOfAny('\r', '\n')) >= 0)
                {
                    output.Write(value[..newline]);
                    value = value.Slice(newline + 1);
                }

                output.Write(value);
            }

            public override void Flush() => output.Flush();
        }
    }
}
