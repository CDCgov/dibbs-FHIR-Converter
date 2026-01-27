// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Specification.Terminology;
using Hl7.Fhir.Support;
using Hl7.Fhir.Utility;
using Hl7.Fhir.Validation;
using Xunit;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests.FilterTests
{
    public class StringFiltersTests
    {

        [Fact]
        public void EscapeSpecialCharsTest()
        {
            Assert.Equal("\\\"", Filters.EscapeSpecialChars("\""));
            Assert.Equal(string.Empty, Filters.EscapeSpecialChars(string.Empty));
            Assert.Null(Filters.EscapeSpecialChars(null));
        }

        [Fact]
        public void MatchTest()
        {
            Assert.Empty(Filters.Match(string.Empty, "[0-9]"));
            Assert.Empty(Filters.Match(null, "[0-9]"));
            Assert.Single(Filters.Match("foo1", "[0-9]"));

            Assert.Throws<ArgumentNullException>(() => Filters.Match("foo1", null));
            Assert.ThrowsAny<ArgumentException>(() => Filters.Match("foo1", "[a-z"));
        }

        [Fact]
        public void ToJsonStringTests()
        {
            Assert.Null(Filters.ToJsonString(null));
            Assert.Equal(@"[""a"",""b""]", Filters.ToJsonString(new List<string>() { "a", "b" }));
        }

        [Fact]
        public void GzipTest()
        {
            // Gzip function is operation system related.
            var actual = Filters.Gzip("uncompressed");
            var expected = new List<string>
            {
                "H4sIAAAAAAAACivNS87PLShKLS5OTQEA3a5CsQwAAAA=",
                "H4sIAAAAAAAAEyvNS87PLShKLS5OTQEA3a5CsQwAAAA=",
                "H4sIAAAAAAAAAyvNS87PLShKLS5OTQEA3a5CsQwAAAA=",
            };
            Assert.Contains(actual, expected);
            Assert.Equal("uncompressed", Filters.GunzipBase64String(actual));
            Assert.Equal(string.Empty, Filters.Gzip(string.Empty));

            Assert.Throws<ArgumentNullException>(() => Filters.Gzip(null));
        }

        public class ToXHtml
        {
            [Fact]
            public void Basic()
            {
                var testString = "<div xmlns=\"http://www.w3.org/1999/xhtml\">Content</div>";
                var result = Filters.ToXhtml(testString);

                Assert.Equal(testString, result);
            }

            [Fact]
            public void MultipleRoot()
            {
                var testString = "<table>Content</table><div>More stuff</div>";
                var expected = $"<div xmlns=\"http://www.w3.org/1999/xhtml\">{testString}</div>";
                var result = Filters.ToXhtml(testString);

                Assert.Equal(expected, result);
            }

            [Fact]
            public void NestedBadTag()
            {
                var testString = "<div xmlns=\"http://www.w3.org/1999/xhtml\"><paragraph>Content</paragraph></div>";
                var expected = "<div xmlns=\"http://www.w3.org/1999/xhtml\"><p>Content</p></div>";
                var result = Filters.ToXhtml(testString);

                Assert.Equal(expected, result);
            }

            [Fact]
            public void TopLevelBadTag()
            {
                var testString = "<paragraph>Content</paragraph>";
                var expected = "<div xmlns=\"http://www.w3.org/1999/xhtml\"><p>Content</p></div>";
                var result = Filters.ToXhtml(testString);

                Assert.Equal(expected, result);
            }

            [Fact]
            public void BadNamespace()
            {
                var testString = "<p xmlns=\"urn:hl7-org:v3\">Content</p>";
                var expected = "<div xmlns=\"http://www.w3.org/1999/xhtml\"><p>Content</p></div>";
                var result = Filters.ToXhtml(testString);

                Assert.Equal(expected, result);
            }

            [Fact]
            public void IdAttribute()
            {
                var testString = "<p ID=\"1234\">Content</p>";
                var expected = $"<div xmlns=\"http://www.w3.org/1999/xhtml\"><p id=\"1234\">Content</p></div>";
                var result = Filters.ToXhtml(testString);

                Assert.Equal(expected, result);
            }

            [Fact]
            public void BadAttribute()
            {
                var testString = "<p TEST=\"test\">Content</p>";
                var expected = $"<div xmlns=\"http://www.w3.org/1999/xhtml\"><p>Content</p></div>";
                var result = Filters.ToXhtml(testString);

                Assert.Equal(expected, result);
            }
        }
    }
}
