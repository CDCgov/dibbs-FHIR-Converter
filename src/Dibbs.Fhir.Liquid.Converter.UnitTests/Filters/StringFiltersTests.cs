// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Fluid;
using Fluid.Values;
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
        private readonly TemplateContext context;

        public StringFiltersTests()
        {
            context = new TemplateContext();
        }

        [Fact]
        public void EscapeSpecialCharsTest()
        {
            Assert.Equal("\\\"", Filters.EscapeSpecialChars(StringValue.Create("\""), FilterArguments.Empty, context).Result.ToStringValue());
            Assert.Equal(string.Empty, Filters.EscapeSpecialChars(StringValue.Create(string.Empty), FilterArguments.Empty, context).Result.ToStringValue());
            Assert.Null(Filters.EscapeSpecialChars(NilValue.Instance, FilterArguments.Empty, context).Result.ToStringValue());
        }

        [Fact]
        public void MatchTest()
        {
            Assert.Empty(Filters.Match(StringValue.Create(string.Empty), new FilterArguments(StringValue.Create("[0-9]")), context).Result.ToStringValue());
            Assert.Empty(Filters.Match(NilValue.Instance, new FilterArguments(StringValue.Create("[0-9]")), context).Result.ToStringValue());
            Assert.Single(Filters.Match(StringValue.Create("foo1"), new FilterArguments(StringValue.Create("[0-9]")), context).Result.ToStringValue());

            Assert.Throws<ArgumentNullException>(() => Filters.Match(StringValue.Create("foo1"), new FilterArguments(NilValue.Instance), context).Result.ToStringValue());
            Assert.ThrowsAny<ArgumentException>(() => Filters.Match(StringValue.Create("foo1"), new FilterArguments(StringValue.Create("[a-z")), context).Result.ToStringValue());
        }

        [Fact]
        public void ToJsonStringTests()
        {
            Assert.Equal(Filters.ToJsonString(NilValue.Instance, FilterArguments.Empty, context).Result, NilValue.Instance);
            Assert.Equal(
                @"[""a"",""b""]",
                Filters.ToJsonString(ArrayValue.Create(new List<string>() { "a", "b" }, new TemplateOptions()), FilterArguments.Empty, context).Result.ToStringValue());
        }

        [Fact]
        public void GzipTest()
        {
            // Gzip function is operation system related.
            var actual = Filters.Gzip(StringValue.Create("uncompressed"), FilterArguments.Empty, context);
            var expected = new List<string>
            {
                "H4sIAAAAAAAACivNS87PLShKLS5OTQEA3a5CsQwAAAA=",
                "H4sIAAAAAAAAEyvNS87PLShKLS5OTQEA3a5CsQwAAAA=",
                "H4sIAAAAAAAAAyvNS87PLShKLS5OTQEA3a5CsQwAAAA=",
            };
            Assert.Contains(actual.Result.ToStringValue(), expected);
            Assert.Equal(string.Empty, Filters.Gzip(StringValue.Create(string.Empty), FilterArguments.Empty, context).Result.ToStringValue());

            Assert.Equal(NilValue.Instance, Filters.Gzip(NilValue.Instance, FilterArguments.Empty, context));
        }

        public class ToXHtml
        {
            private readonly TemplateContext context;
            public ToXHtml()
            {
                context = new TemplateContext();
            }

            [Fact]
            public void Basic()
            {
                var testString = "<div xmlns=\"http://www.w3.org/1999/xhtml\">Content</div>";
                var result = Filters.ToXhtml(StringValue.Create(testString), FilterArguments.Empty, context).Result.ToStringValue();

                Assert.Equal(testString, result);
            }

            [Fact]
            public void MultipleRoot()
            {
                var testString = "<table>Content</table><div>More stuff</div>";
                var expected = $"<div xmlns=\"http://www.w3.org/1999/xhtml\">{testString}</div>";
                var result = Filters.ToXhtml(StringValue.Create(testString), FilterArguments.Empty, context).Result.ToStringValue();

                Assert.Equal(expected, result);
            }

            [Fact]
            public void NestedBadTag()
            {
                var testString = "<div xmlns=\"http://www.w3.org/1999/xhtml\"><paragraph>Content</paragraph></div>";
                var expected = "<div xmlns=\"http://www.w3.org/1999/xhtml\"><p>Content</p></div>";
                var result = Filters.ToXhtml(StringValue.Create(testString), FilterArguments.Empty, context).Result.ToStringValue();

                Assert.Equal(expected, result);
            }

            [Fact]
            public void TopLevelBadTag()
            {
                var testString = "<paragraph>Content</paragraph>";
                var expected = "<div xmlns=\"http://www.w3.org/1999/xhtml\"><p>Content</p></div>";
                var result = Filters.ToXhtml(StringValue.Create(testString), FilterArguments.Empty, context).Result.ToStringValue();

                Assert.Equal(expected, result);
            }

            [Fact]
            public void BadNamespace()
            {
                var testString = "<p xmlns=\"urn:hl7-org:v3\">Content</p>";
                var expected = "<div xmlns=\"http://www.w3.org/1999/xhtml\"><p>Content</p></div>";
                var result = Filters.ToXhtml(StringValue.Create(testString), FilterArguments.Empty, context).Result.ToStringValue();

                Assert.Equal(expected, result);
            }

            [Fact]
            public void IdAttribute()
            {
                var testString = "<p ID=\"1234\">Content</p>";
                var expected = $"<div xmlns=\"http://www.w3.org/1999/xhtml\"><p id=\"1234\">Content</p></div>";
                var result = Filters.ToXhtml(StringValue.Create(testString), FilterArguments.Empty, context).Result.ToStringValue();

                Assert.Equal(expected, result);
            }

            [Fact]
            public void BadAttribute()
            {
                var testString = "<p TEST=\"test\">Content</p>";
                var expected = $"<div xmlns=\"http://www.w3.org/1999/xhtml\"><p>Content</p></div>";
                var result = Filters.ToXhtml(StringValue.Create(testString), FilterArguments.Empty, context).Result.ToStringValue();

                Assert.Equal(expected, result);
            }
        }
    }
}
