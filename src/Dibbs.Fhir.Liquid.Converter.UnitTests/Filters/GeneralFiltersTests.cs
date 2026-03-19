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
        public static IEnumerable<object[]> GetValidDataForGenerateUuid()
        {
            yield return new FluidValue[] { NilValue.Instance, NilValue.Instance };
            yield return new FluidValue[] { StringValue.Create(string.Empty), NilValue.Instance };
            yield return new FluidValue[] { StringValue.Create("  \n"), NilValue.Instance };
            yield return new FluidValue[] { StringValue.Create("MRN12345"), StringValue.Create("e7ce584a-acf4-7cf0-5b4e-d4961c8123e2") };
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
            FluidValue input,
            FluidValue expected
        )
        {
            var context = new TemplateContext();
            Assert.Equal(expected, Filters.GenerateUUID(input, FilterArguments.Empty, context).Result);
        }

        [Fact]
        public void PrependIdTest_uuid()
        {
            string id = "fc97958d-4b72-47a4-887f-b14ff8bcc859";
            var context = new TemplateContext();
            Assert.Equal("urn:uuid:" + id, Filters.PrependID(StringValue.Create(id), FilterArguments.Empty, context).Result.ToStringValue());
        }
        [Fact]
        public void PrependIdTest_uuid_uppercase()
        {
            string id = "FC97958D-4B72-47A4-887F-B14FF8BCC859";
            var context = new TemplateContext();
            Assert.Equal("urn:uuid:" + id.ToLower(), Filters.PrependID(StringValue.Create(id), FilterArguments.Empty, context).Result.ToStringValue());
        }

        [Fact]
        public void PrependIdTest_oid()
        {
            string id = "1.3.6.1.4.1.343";
            var context = new TemplateContext();
            Assert.Equal("urn:oid:" + id, Filters.PrependID(StringValue.Create(id), FilterArguments.Empty, context).Result.ToStringValue());
        }

        [Fact]
        public void PrependIdTest_unknown()
        {
            string id = "a random ID";
            var context = new TemplateContext();
            Assert.Equal(id, Filters.PrependID(StringValue.Create(id), FilterArguments.Empty, context).Result.ToStringValue());
        }

        [Fact]
        public void RemovePrefix()
        {
            string extension = "http://example.com/user/1";
            string root = "http://example.com/user";
            var context = new TemplateContext();

            Assert.Equal("1", Filters.RemovePrefix(StringValue.Create(extension), new FilterArguments(StringValue.Create(root)), context).Result.ToStringValue());
        }

        [Fact]
        public void RemovePrefixExtensionNotUrl()
        {
            string extension = "not url";
            string root = "\"http://example.com/user\"";
            var context = new TemplateContext();

            Assert.Equal(extension, Filters.RemovePrefix(StringValue.Create(extension), new FilterArguments(StringValue.Create(root)), context).Result.ToStringValue());
        }

        [Fact]
        public void RemovePrefixRootNotUrl()
        {
            string extension = "http://example.com/user/1";
            string root = "\"not url\"";
            var context = new TemplateContext();

            Assert.Equal(extension, Filters.RemovePrefix(StringValue.Create(extension), new FilterArguments(StringValue.Create(root)), context).Result.ToStringValue());
        }

        [Fact]
        public void RemovePrefixRootNoSlash()
        {
            string extension = "http://example.com/user";
            string root = "\"http://example.com/use\"";
            var context = new TemplateContext();

            Assert.Equal(extension, Filters.RemovePrefix(StringValue.Create(extension), new FilterArguments(StringValue.Create(root)), context).Result.ToStringValue());
        }

        [Fact]
        public void RemovePrefixRootExtensionWithMultipleSegments()
        {
            string extension = "http://example.com/category/user/1";
            string root = "http://example.com";

            string expected = "category/user/1";
            var context = new TemplateContext();

            Assert.Equal(expected, Filters.RemovePrefix(StringValue.Create(extension), new FilterArguments(StringValue.Create(root)), context).Result.ToStringValue());
        }

        [Fact]
        public void RemovePrefixRootRootWithMultipleSegments()
        {
            string extension = "http://example.com/category/user/1";
            string root = "http://example.com/category";

            string expected = "user/1";
            var context = new TemplateContext();

            Assert.Equal(expected, Filters.RemovePrefix(StringValue.Create(extension), new FilterArguments(StringValue.Create(root)), context).Result.ToStringValue());
        }

        [Fact]
        public void FormatValueQuantity()
        {
            var context = new TemplateContext();

            Assert.Equal("0.29", Filters.FormatValueQuantity(StringValue.Create(".29"), FilterArguments.Empty, context).Result.ToStringValue());
            Assert.Equal("300", Filters.FormatValueQuantity(StringValue.Create("300"), FilterArguments.Empty, context).Result.ToStringValue());
            Assert.True(Filters.FormatValueQuantity(StringValue.Create(".50 in"), FilterArguments.Empty, context).Result.IsNil());
            Assert.Equal("300.00", Filters.FormatValueQuantity(StringValue.Create("300.00"), FilterArguments.Empty, context).Result.ToStringValue());
            Assert.Equal("-300", Filters.FormatValueQuantity(StringValue.Create("-300"), FilterArguments.Empty, context).Result.ToStringValue());
        }

        [Fact]
        public void ExtractUnit()
        {
            var context = new TemplateContext();

            Assert.Equal("in", Filters.ExtractUnit(StringValue.Create("12 in"), FilterArguments.Empty, context).Result.ToStringValue());
            Assert.Equal("$", Filters.ExtractUnit(StringValue.Create("$1"), FilterArguments.Empty, context).Result.ToStringValue());
            Assert.True(Filters.ExtractUnit(StringValue.Create("1"), FilterArguments.Empty, context).Result.IsNil());
        }
    }
}
