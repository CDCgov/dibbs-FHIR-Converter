// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Antlr4.Runtime;
using Dibbs.Fhir.Liquid.Converter.Exceptions;
using Dibbs.Fhir.Liquid.Converter.Models;
using Fluid;
using Fluid.Values;
using Xunit;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests.FilterTests
{
    public class CollectionFiltersTest
    {
        private readonly TemplateContext context;
        private readonly FluidParser parser;
        public CollectionFiltersTest()
        {
            context = new TemplateContext();
            parser = new FluidParser();
        }

        [Fact]
        public void ToArrayTests()
        {
            Assert.Equal(0, (Filters.ToArray(NilValue.Instance, FilterArguments.Empty, context).Result as ArrayValue).Values.Count);
            Assert.Equal(1, (Filters.ToArray(NumberValue.Create(1), FilterArguments.Empty, context).Result as ArrayValue).Values.Count);
            Assert.Equal(2, (Filters.ToArray(ArrayValue.Create(new List<string> { null, string.Empty }, new TemplateOptions()), FilterArguments.Empty, context).Result as ArrayValue).Values.Count);
        }

        [Fact]
        public void BatchRenderTests()
        {
            // Valid template file system and template
            var templateCollection = new List<Dictionary<string, IFluidTemplate>>
            {
                new Dictionary<string, IFluidTemplate>
                {
                    { "foo", parser.Parse("{{ i }} ") },
                },
            };

            var templateProvider = new TemplateProvider(templateCollection);
            var context = new TemplateContext();
            context.SetValue("file_system", templateProvider.GetTemplateFileSystem());

            var collection = new List<object> { 1, 2, 3 };
            Assert.Equal("1 ,2 ,3 ,", Filters.BatchRender(ArrayValue.Create(collection, new TemplateOptions()), new FilterArguments([StringValue.Create("foo"), StringValue.Create("i")]), context).Result.ToStringValue());

            // Valid template file system but null collection
            Assert.Equal(string.Empty, Filters.BatchRender(NilValue.Instance,  new FilterArguments([StringValue.Create("foo"), StringValue.Create("i")]), context).Result.ToStringValue());

            // No template file system
            context = new TemplateContext(CultureInfo.InvariantCulture);
            var exception = Assert.Throws<RenderException>(() => Filters.BatchRender(NilValue.Instance, new FilterArguments([StringValue.Create("foo"), StringValue.Create("bar")]), context));
            Assert.Equal(FhirConverterErrorCode.TemplateNotFound, exception.FhirConverterErrorCode);

            // Valid template file system but non-existing template
            exception = Assert.Throws<RenderException>(() => Filters.BatchRender(ArrayValue.Create(collection, new TemplateOptions()), new FilterArguments([StringValue.Create("bar"), StringValue.Create("i")]), context));
            Assert.Equal(FhirConverterErrorCode.TemplateNotFound, exception.FhirConverterErrorCode);
        }

        [Fact]
        public void NestedWhere_NullData_ReturnsNull()
        {
            var actual = Filters.NestedWhere(NilValue.Instance, new FilterArguments(StringValue.Create("test.path")), context).Result;
            Assert.Equal(NilValue.Instance, actual);
        }

        [Fact]
        public void NestedWhere_EmptyArray_ReturnsEmpty()
        {
            var actual = Filters.NestedWhere(ArrayValue.Empty, new FilterArguments(StringValue.Create("test.path")), context).Result as ArrayValue;
            Assert.Equal(0, actual.Values.Count);
        }

        [Fact]
        public void NestedWhere_NoMatch_ReturnsEmpty()
        {
            var actual = Filters.NestedWhere(
              ArrayValue.Create(new object[] { new { test = "hi" }, new { test = "bye" } }, new TemplateOptions()),
              new FilterArguments(StringValue.Create("test.path")), context).Result as ArrayValue;
            Assert.Equal(0, actual.Values.Count);
        }

        [Fact]
        public void NestedWhere_Match_ReturnsMatch()
        {
            var actual = Filters.NestedWhere(
              ArrayValue.Create(new object[] { new { test = new { path = "hi" } }, new { test = "bye" } }, new TemplateOptions()),
              new FilterArguments(StringValue.Create("test.path")), context).Result as ArrayValue;
            Assert.Equal(ArrayValue.Create(new object[] { new { test = new { path = "hi" } } }, new TemplateOptions()), actual);
        }

        [Fact]
        public void NestedWhere_MatchButNotValue_ReturnsEmpty()
        {
            var actual = Filters.NestedWhere(
              ArrayValue.Create(new object[] { new { test = new { path = "hi" } }, new { test = "bye" } }, new TemplateOptions()),
              new FilterArguments(StringValue.Create("test.path"), StringValue.Create("other")), context).Result as ArrayValue;
            Assert.Equal(ArrayValue.Empty, actual);
        }

        [Fact]
        public void NestedWhere_MatchIncludingValue_ReturnsMatch()
        {
            var actual = Filters.NestedWhere(
              ArrayValue.Create(new object[] { new { test = new { path = "hi" } }, new { test = "bye" } }, new TemplateOptions()),
              new FilterArguments(StringValue.Create("test.path"), StringValue.Create("hi")), context).Result as ArrayValue;
            Assert.Equal(ArrayValue.Create(new object[] { new { test = new { path = "hi" } } }, new TemplateOptions()), actual);
        }

        [Fact]
        public void NestedWhere_MatchIncludingValueList_ReturnsMatch()
        {
            var actual = Filters.NestedWhere(
            ArrayValue.Create(
                new object[] {
                    new { test = new object[] { new { path = "hi" } , new { path = "nope"} } },
                    new { test = "bye" }
                },
                new TemplateOptions()),
            new FilterArguments(StringValue.Create("test.path"), StringValue.Create("hi")), context).Result as ArrayValue;
            Assert.Equal(1, actual.Values.Count);
        }
    }
}
