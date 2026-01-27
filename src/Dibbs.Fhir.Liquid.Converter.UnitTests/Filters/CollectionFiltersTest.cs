// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using DotLiquid;
using Microsoft.Health.Fhir.Liquid.Converter.Exceptions;
using Microsoft.Health.Fhir.Liquid.Converter.Models;
using Xunit;

namespace Microsoft.Health.Fhir.Liquid.Converter.UnitTests.FilterTests
{
    public class CollectionFiltersTest
    {
        [Fact]
        public void ToArrayTests()
        {
            Assert.Empty(Filters.ToArray(null));
            Assert.Single(Filters.ToArray(1));
            Assert.Equal(2, Filters.ToArray(new List<string> { null, string.Empty }).Count);
        }

        [Fact]
        public void BatchRenderTests()
        {
            // Valid template file system and template
            var templateCollection = new List<Dictionary<string, Template>>
            {
                new Dictionary<string, Template>
                {
                    { "foo", Template.Parse("{{ i }} ") },
                },
            };

            var templateProvider = new TemplateProvider(templateCollection);
            var context = new Context(
                environments: new List<Hash>(),
                outerScope: new Hash(),
                registers: Hash.FromDictionary(new Dictionary<string, object>() { { "file_system", templateProvider.GetTemplateFileSystem() } }),
                errorsOutputMode: ErrorsOutputMode.Rethrow,
                maxIterations: 0,
                formatProvider: CultureInfo.InvariantCulture,
                cancellationToken: CancellationToken.None);

            var collection = new List<object> { 1, 2, 3 };
            Assert.Equal("1 ,2 ,3 ,", Filters.BatchRender(context, collection, "foo", "i"));
            var result = Filters.BatchRenderParallel(context, collection, "foo", "i");
            Assert.Contains("1 ", result);
            Assert.Contains("2 ", result);
            Assert.Contains("3 ", result);

            // Valid template file system but null collection
            Assert.Equal(string.Empty, Filters.BatchRender(context, null, "foo", "i"));
            Assert.Equal(string.Empty, Filters.BatchRenderParallel(context, null, "foo", "i"));

            // No template file system
            context = new Context(CultureInfo.InvariantCulture);
            var exception = Assert.Throws<RenderException>(() => Filters.BatchRender(context, null, "foo", "bar"));
            Assert.Equal(FhirConverterErrorCode.TemplateNotFound, exception.FhirConverterErrorCode);
            exception = Assert.Throws<RenderException>(() => Filters.BatchRenderParallel(context, null, "foo", "bar"));
            Assert.Equal(FhirConverterErrorCode.TemplateNotFound, exception.FhirConverterErrorCode);

            // Valid template file system but non-existing template
            exception = Assert.Throws<RenderException>(() => Filters.BatchRender(context, collection, "bar", "i"));
            Assert.Equal(FhirConverterErrorCode.TemplateNotFound, exception.FhirConverterErrorCode);
            exception = Assert.Throws<RenderException>(() => Filters.BatchRenderParallel(context, collection, "bar", "i"));
            Assert.Equal(FhirConverterErrorCode.TemplateNotFound, exception.FhirConverterErrorCode);
        }

        [Fact]
        public void NestedWhere_NullData_ReturnsNull()
        {
            var actual = Filters.NestedWhere(null, "test.path");
            Assert.Equal(null, actual);
        }

        [Fact]
        public void NestedWhere_EmptyArray_ReturnsEmpty()
        {
            var actual = Filters.NestedWhere(new object[] { }, "test.path");
            Assert.Equal(new object[] { }, actual);
        }

        [Fact]
        public void NestedWhere_NoMatch_ReturnsEmpty()
        {
            var actual = Filters.NestedWhere(
              new object[] { new { test = "hi" }, new { test = "bye" } },
              "test.path");
            Assert.Equal(new object[] { }, actual);
        }

        [Fact]
        public void NestedWhere_Match_ReturnsMatch()
        {
            var actual = Filters.NestedWhere(
              new object[] { new { test = new { path = "hi" } }, new { test = "bye" } },
              "test.path");
            Assert.Equal(new object[] { new { test = new { path = "hi" } } }, actual);
        }

        [Fact]
        public void NestedWhere_MatchButNotValue_ReturnsEmpty()
        {
            var actual = Filters.NestedWhere(
              new object[] { new { test = new { path = "hi" } }, new { test = "bye" } },
              "test.path",
              "other");
            Assert.Equal(new object[] { }, actual);
        }

        [Fact]
        public void NestedWhere_MatchIncludingValue_ReturnsMatch()
        {
            var actual = Filters.NestedWhere(
              new object[] { new { test = new { path = "hi" } }, new { test = "bye" } },
              "test.path",
              "hi");
            Assert.Equal(new object[] { new { test = new { path = "hi" } } }, actual);
        }

        [Fact]
        public void NestedWhere_MatchIncludingValueList_ReturnsMatch()
        {
            var actual = Filters.NestedWhere(
              new object[] {
            new { test = new object[] { new { path = "hi" } , new { path = "nope"} } },
            new { test = "bye" }
              },
              "test.path",
              "hi");
            Assert.Equal(1, actual.Count());
        }
    }
}
