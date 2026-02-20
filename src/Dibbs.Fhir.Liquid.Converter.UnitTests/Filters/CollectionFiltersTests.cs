// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Dibbs.Fhir.Liquid.Converter.Exceptions;
using Dibbs.Fhir.Liquid.Converter.Models;
using Fluid;
using Fluid.Values;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Interfaces;
using Xunit;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests.FilterTests
{
    public class CollectionFiltersTests
    {
        private readonly FluidParser parser;
        public CollectionFiltersTests()
        {
            parser = new FluidParser();
        }

        [Fact]
        public async void ToArrayTests()
        {
            var context = new TemplateContext();

            Assert.Equal(0, (await Filters.ToArray(NilValue.Instance, FilterArguments.Empty, context) as ArrayValue).Values.Count);
            Assert.Equal(1, (await Filters.ToArray(NumberValue.Create(1), FilterArguments.Empty, context) as ArrayValue).Values.Count);
            Assert.Equal(2, (await Filters.ToArray(ArrayValue.Create(new List<string> { null, string.Empty }, new TemplateOptions()), FilterArguments.Empty, context) as ArrayValue).Values.Count);
        }

        [Fact]
        public async void BatchRenderTests()
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
            Assert.Equal("1 ,2 ,3 ,", (await Filters.BatchRender(ArrayValue.Create(collection, new TemplateOptions()), new FilterArguments([StringValue.Create("foo"), StringValue.Create("i")]), context)).ToStringValue());
            // Valid template file system but null collection
            Assert.Equal(string.Empty, (await Filters.BatchRender(NilValue.Instance,  new FilterArguments([StringValue.Create("foo"), StringValue.Create("i")]), context)).ToStringValue());

            // No template file system
            context = new TemplateContext(CultureInfo.InvariantCulture);
            var exception = await Assert.ThrowsAsync<RenderException>(async () => await Filters.BatchRender(NilValue.Instance, new FilterArguments([StringValue.Create("foo"), StringValue.Create("bar")]), context));
            Assert.Equal(FhirConverterErrorCode.TemplateNotFound, exception.FhirConverterErrorCode);

            // Valid template file system but non-existing template
            exception = await Assert.ThrowsAsync<RenderException>(async () => await Filters.BatchRender(ArrayValue.Create(collection, new TemplateOptions()), new FilterArguments([StringValue.Create("bar"), StringValue.Create("i")]), context));
            Assert.Equal(FhirConverterErrorCode.TemplateNotFound, exception.FhirConverterErrorCode);
        }

        [Fact]
        public async void NestedWhere_NullData_ReturnsNull()
        {
            var context = new TemplateContext();
            var actual = await Filters.NestedWhere(NilValue.Instance, new FilterArguments(StringValue.Create("test.path")), context);
            Assert.Equal(NilValue.Instance, actual);
        }

        [Fact]
        public async void NestedWhere_EmptyArray_ReturnsEmpty()
        {
            var context = new TemplateContext();
            var actual = await Filters.NestedWhere(ArrayValue.Empty, new FilterArguments(StringValue.Create("test.path")), context) as ArrayValue;
            Assert.Equal(0, actual.Values.Count);
        }

        [Fact]
        public async void NestedWhere_NoMatch_ReturnsEmpty()
        {
            var context = new TemplateContext();
            var actual = await Filters.NestedWhere(
              ArrayValue.Create(new object[] { new { test = "hi" }, new { test = "bye" } }, new TemplateOptions()),
              new FilterArguments(StringValue.Create("test.path")), context) as ArrayValue;
            Assert.Equal(0, actual.Values.Count);
        }

        [Fact]
        public async void NestedWhere_Match_ReturnsMatch()
        {
            var context = new TemplateContext();
            Dictionary<string, object>[] inputCollection = [ 
                new Dictionary<string, object> { 
                    { 
                        "test", 
                        new Dictionary<string, object> { 
                            { "path", "hi" },
                        }
                    }
                },     
                new Dictionary<string, object> { 
                    { "test", "bye" } 
                }
            ];
            Dictionary<string, object>[] expectedCollection = [
                new Dictionary<string, object> { 
                    { "test", new Dictionary<string, object> { 
                        { "path", "hi" } 
                    } 
                } }];
            var expected = ArrayValue.Create(expectedCollection, new TemplateOptions());

            var actual = await Filters.NestedWhere(
              ArrayValue.Create(inputCollection, new TemplateOptions()),
              new FilterArguments(StringValue.Create("test.path")), context) as ArrayValue;
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async void NestedWhere_MatchButNotValue_ReturnsEmpty()
        {
            var context = new TemplateContext();
            var actual = await Filters.NestedWhere(
              ArrayValue.Create(new object[] { new { test = new { path = "hi" } }, new { test = "bye" } }, new TemplateOptions()),
              new FilterArguments(StringValue.Create("test.path"), StringValue.Create("other")), context) as ArrayValue;
            Assert.Equal(ArrayValue.Empty, actual);
        }

        [Fact]
        public async void NestedWhere_MatchIncludingValue_ReturnsMatch()
        {
            var context = new TemplateContext();
            Dictionary<string, object>[] inputCollection = [ 
                                new Dictionary<string, object> { 
                                    { "test", new Dictionary<string, object> { 
                                        { "path", "hi" } } 
                                    } }, 
                                    new Dictionary<string, object> { 
                                        { "test", "bye" } 
                                    }
                                ];
                                
            Dictionary<string, object>[] expectedCollection = [
                                new Dictionary<string, object> { 
                                    { "test", new Dictionary<string, object> { 
                                        { "path", "hi" } 
                                    } 
                                } }];

            var expected = ArrayValue.Create(expectedCollection, new TemplateOptions());
            var actual = await Filters.NestedWhere(
              ArrayValue.Create(inputCollection, new TemplateOptions()),
              new FilterArguments(StringValue.Create("test.path"), StringValue.Create("hi")), context) as ArrayValue;
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async void NestedWhere_MatchIncludingValueList_ReturnsMatch()
        {
            var context = new TemplateContext();

            object[] path = [
                new Dictionary<string, object> { 
                    { "path", "hi" },
                },
                new Dictionary<string, object> { 
                    { "path", "nope" },
                }      
            ];
            Dictionary<string, object>[] inputCollection = [ 
                new Dictionary<string, object> { 
                    { "test", path }
                },     
                new Dictionary<string, object> { 
                    { "test", "bye" } 
                }
            ];
            var actual = await Filters.NestedWhere(
                ArrayValue.Create(inputCollection, new TemplateOptions()),
                new FilterArguments(StringValue.Create("test.path"), StringValue.Create("hi")), context) as ArrayValue;
            Assert.Equal(1, actual.Values.Count);
        }
    }
}
