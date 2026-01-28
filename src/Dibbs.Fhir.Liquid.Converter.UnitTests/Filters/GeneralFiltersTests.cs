// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Dibbs.Fhir.Liquid.Converter.Exceptions;
using Dibbs.Fhir.Liquid.Converter.Models;
using Fluid;
using Fluid.Values;
using Xunit;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests.FilterTests
{
    public class GeneralFiltersTests
    {
        private readonly TemplateContext context;

        public GeneralFiltersTests()
        {
            context = new TemplateContext();
        }

        public static IEnumerable<object[]> GetValidDataForGenerateUuid()
        {
            yield return new object[] { null, null };
            yield return new object[] { string.Empty, null };
            yield return new object[] { "  \n", null };
            yield return new object[] { "MRN12345", "e7ce584a-acf4-7cf0-5b4e-d4961c8123e2" };
        }

        [Fact]
        public void GetPropertyTest()
        {
            // empty context
            var context = new TemplateContext(CultureInfo.InvariantCulture);
            Assert.Equal(Filters.GetProperty(NilValue.Instance, FilterArguments.Empty, context).Result, NilValue.Instance);

            // context with null CodeMapping
            context.SetValue("CodeMapping", NilValue.Instance);
            Assert.Equal("M", Filters.GetProperty(StringValue.Create("M"), new FilterArguments(StringValue.Create("Gender")), context).Result.ToStringValue());

            // context with valid CodeMapping
            context.SetValue("CodeMapping",
                new CodeMapping(
                    new Dictionary<string, Dictionary<string, Dictionary<string, string>>>
                    {
                        {
                            "CodeSystem/Gender",
                            new Dictionary<string, Dictionary<string, string>>
                            {
                                {
                                    "M",
                                    new Dictionary<string, string> { { "code", "male" } }
                                },
                            }
                        },
                    }
                )
            );
            Assert.Equal("male", Filters.GetProperty(StringValue.Create("M"), new FilterArguments(StringValue.Create("CodeSystem/Gender")), context).Result.ToStringValue());
            Assert.Equal(NilValue.Instance, Filters.GetProperty(StringValue.Create("M"), FilterArguments.Empty, context).Result);
            Assert.Equal(NilValue.Instance, Filters.GetProperty(StringValue.Create("M"), new FilterArguments(StringValue.Create(string.Empty)), context).Result);
        }

        [Theory]
        [MemberData(nameof(GetValidDataForGenerateUuid))]
        public void GivenValidData_WhenGenerateUuid_CorrectResultShouldBeReturned(
            string input,
            string expected
        )
        {
            Assert.Equal(expected, Filters.GenerateUUID(StringValue.Create(input), FilterArguments.Empty, context).Result.ToStringValue());
        }

        [Fact]
        public void PrependIdTest_uuid()
        {
            string id = "fc97958d-4b72-47a4-887f-b14ff8bcc859";
            Assert.Equal("urn:uuid:" + id, Filters.PrependID(StringValue.Create(id), FilterArguments.Empty, context).Result.ToStringValue());
        }
        [Fact]
        public void PrependIdTest_uuid_uppercase()
        {
            string id = "FC97958D-4B72-47A4-887F-B14FF8BCC859";
            Assert.Equal("urn:uuid:" + id.ToLower(), Filters.PrependID(StringValue.Create(id), FilterArguments.Empty, context).Result.ToStringValue());
        }

        [Fact]
        public void PrependIdTest_oid()
        {
            string id = "1.3.6.1.4.1.343";
            Assert.Equal("urn:oid:" + id, Filters.PrependID(StringValue.Create(id), FilterArguments.Empty, context).Result.ToStringValue());
        }

        [Fact]
        public void PrependIdTest_unknown()
        {
            string id = "a random ID";
            Assert.Equal(id, Filters.PrependID(StringValue.Create(id), FilterArguments.Empty, context).Result.ToStringValue());
        }

        [Fact]
        public void RemovePrefix()
        {
            string extension = "http://example.com/user/1";
            string root = "http://example.com/user";

            Assert.Equal("1", Filters.RemovePrefix(StringValue.Create(extension), new FilterArguments(StringValue.Create(root)), context).Result.ToStringValue());
        }

        [Fact]
        public void RemovePrefixExtensionNotUrl()
        {
            string extension = "not url";
            string root = "\"http://example.com/user\"";

            Assert.Equal(extension, Filters.RemovePrefix(StringValue.Create(extension), new FilterArguments(StringValue.Create(root)), context).Result.ToStringValue());
        }

        [Fact]
        public void RemovePrefixRootNotUrl()
        {
            string extension = "http://example.com/user/1";
            string root = "\"not url\"";

            Assert.Equal(extension, Filters.RemovePrefix(StringValue.Create(extension), new FilterArguments(StringValue.Create(root)), context).Result.ToStringValue());
        }

        [Fact]
        public void RemovePrefixRootNoSlash()
        {
            string extension = "http://example.com/user";
            string root = "\"http://example.com/use\"";

            Assert.Equal(extension, Filters.RemovePrefix(StringValue.Create(extension), new FilterArguments(StringValue.Create(root)), context).Result.ToStringValue());
        }

        [Fact]
        public void RemovePrefixRootExtensionWithMultipleSegments()
        {
            string extension = "http://example.com/category/user/1";
            string root = "http://example.com";

            string expected = "category/user/1";

            Assert.Equal(expected, Filters.RemovePrefix(StringValue.Create(extension), new FilterArguments(StringValue.Create(root)), context).Result.ToStringValue());
        }

        [Fact]
        public void RemovePrefixRootRootWithMultipleSegments()
        {
            string extension = "http://example.com/category/user/1";
            string root = "http://example.com/category";

            string expected = "user/1";

            Assert.Equal(expected, Filters.RemovePrefix(StringValue.Create(extension), new FilterArguments(StringValue.Create(root)), context).Result.ToStringValue());
        }
    }
}
