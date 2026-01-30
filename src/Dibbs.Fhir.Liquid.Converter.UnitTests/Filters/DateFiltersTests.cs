// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Dibbs.Fhir.Liquid.Converter.Exceptions;
using Dibbs.Fhir.Liquid.Converter.Models;
using Dibbs.Fhir.Liquid.Converter.DataParsers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Fluid.Values;
using Fluid;
using Dibbs.Fhir.Liquid.Converter.Utilities;
using System.Net.Http.Headers;
using Fluid.Ast;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests.FilterTests
{
    public class DateFiltersTests
    {
        private readonly TemplateContext context;
        public DateFiltersTests()
        {
            context = new TemplateContext();
        }

        public static IEnumerable<object[]> GetValidDataForAddHyphensDate()
        {
            yield return new object[] { NilValue.Instance, "local", NilValue.Instance };
            yield return new object[] { StringValue.Empty, "local", StringValue.Empty };
            yield return new object[] { StringValue.Create("2001"), "preserve", StringValue.Create("2001") };
            yield return new object[] { StringValue.Create("200101"), "preserve", StringValue.Create("2001-01") };
            yield return new object[] { StringValue.Create("19241010"), "local", StringValue.Create("1924-10-10") };
            yield return new object[] { StringValue.Create("19850101000000"), "local", StringValue.Create("1985-01-01") };
            yield return new object[] { StringValue.Create("19850101000000.1234"), "local", StringValue.Create("1985-01-01") };
        }

        // We assume the local timezone is +08:00.
        public static IEnumerable<object[]> GetValidDataWithoutTimeZoneForAddHyphensDateWithUtcTimeZoneHandling()
        {
            yield return new object[] { @"200101", "utc", new DateTime(2001, 1, 1) };
            yield return new object[] { @"20010102", "utc", new DateTime(2001, 1, 2) };
            yield return new object[] { @"19880101000000", "utc", new DateTime(1988, 1, 1, 0, 0, 0) };
            yield return new object[] { @"19880101000000.1234", "utc", new DateTime(1988, 1, 1, 0, 0, 0) };
        }

        public static IEnumerable<object[]> GetValidDataForAddHyphensDateWithDefaultTimeZoneHandling()
        {
            yield return new object[] { NilValue.Instance, NilValue.Instance };
            yield return new object[] { StringValue.Empty, StringValue.Empty };
            yield return new object[] { StringValue.Create("2001"), StringValue.Create("2001") };
            yield return new object[] { StringValue.Create("200101"), StringValue.Create("2001-01") };
            yield return new object[] { StringValue.Create("19241010"), StringValue.Create("1924-10-10") };
            yield return new object[] { StringValue.Create("19850101000000"), StringValue.Create("1985-01-01") };
            yield return new object[] { StringValue.Create("19850101000000.1234"), StringValue.Create("1985-01-01") };
        }

        public static IEnumerable<object[]> GetValidDataForFormatAsDateTime()
        {
            // TimeZoneHandling does not affect dateTime without time
            yield return new object[] { NilValue.Instance, "preserve", NilValue.Instance };
            yield return new object[] { NilValue.Instance, "utc", NilValue.Instance };
            yield return new object[] { NilValue.Instance, "local", NilValue.Instance };
            yield return new object[] { StringValue.Empty, "preserve", StringValue.Empty };
            yield return new object[] { StringValue.Empty, "utc", StringValue.Empty };
            yield return new object[] { StringValue.Empty, "local", StringValue.Empty };
            yield return new object[] { StringValue.Create("2001"), "preserve", StringValue.Create("2001") };
            yield return new object[] { StringValue.Create("2001"), "local", StringValue.Create("2001") };
            yield return new object[] { StringValue.Create("200101"), "preserve", StringValue.Create("2001-01") };
            yield return new object[] { StringValue.Create("200101"), "local", StringValue.Create("2001-01") };

            // If time zone provided, it should be formatted according to TimeZoneHandling
            yield return new object[] { StringValue.Create("20110103143428-0800"), "preserve", StringValue.Create("2011-01-03T14:34:28-08:00") };
            yield return new object[] { StringValue.Create("20110103143428-0845"), "preserve", StringValue.Create("2011-01-03T14:34:28-08:45") };
            yield return new object[] { StringValue.Create("20110103143428-0800"), "utc", StringValue.Create("2011-01-03T22:34:28Z") };
            yield return new object[] { StringValue.Create("20110103143428-0845"), "utc", StringValue.Create("2011-01-03T23:19:28Z") };

            yield return new object[] { StringValue.Create("19701231115959+0600"), "preserve", StringValue.Create("1970-12-31T11:59:59+06:00") };
            yield return new object[] { StringValue.Create("19701231115959+0600"), "utc", StringValue.Create("1970-12-31T05:59:59Z") };
            yield return new object[] { StringValue.Create("19701231115959+0630"), "utc", StringValue.Create("1970-12-31T05:29:59Z") };
            yield return new object[] { StringValue.Create("19701231115959.12234+0630"), "utc", StringValue.Create("1970-12-31T05:29:59.12234Z") };
            yield return new object[] { StringValue.Create("19701231115959.000+0630"), "utc", StringValue.Create("1970-12-31T05:29:59.000Z") };

            // TODO: Remove?
            // Skip this test in pipeline, as the local time zone is different
            // yield return new object[] { @"20110103143428-0800", "local", @"2011-01-04T06:34:28+08:00" };
            // yield return new object[] { @"19701231115959+0600", "local", @"1970-12-31T13:59:59+08:00" };

            // If no time zone provided, it is treated as local
            // yield return new object[] { @"2001", "utc", @"2000" };
            // yield return new object[] { @"20050110045253", "utc", @"2005-01-09T20:52:53Z" };
            // yield return new object[] { @"20050110045253", "preserve", @"2005-01-10T04:52:53+08:00" };
            // yield return new object[] { @"20050110045253", "local", @"2005-01-10T04:52:53+08:00" };
        }

        // We assume the local timezone is +08:00.
        public static IEnumerable<object[]> GetValidDataWithoutTimeZoneForFormatAsDateTimeWithUtcTimeZoneHandling()
        {
            yield return new object[] { @"20050110045253", "utc", @"2005-01-09T20:52:53Z", new DateTime(2005, 1, 10, 4, 52, 53) };
            yield return new object[] { @"19880103143428", "utc", @"1988-01-03T06:34:28Z", new DateTime(1988, 1, 3, 14, 34, 28) };
            yield return new object[] { @"19701231115959", "utc", @"1970-12-31T03:59:59Z", new DateTime(1970, 12, 31, 11, 59, 59) };
        }

        public static IEnumerable<object[]> GetValidDataForFormatAsDateTimeWithDefaultTimeZoneHandling()
        {
            yield return new object[] { @"200101", @"2001-01" };
            yield return new object[] { @"20110103143428-0800", @"2011-01-03T14:34:28-08:00" };
            yield return new object[] { @"19701231115959+0600", @"1970-12-31T11:59:59+06:00" };
            yield return new object[] { @"19701231115959+0600", @"1970-12-31T11:59:59+06:00" };
            yield return new object[] { @"19701231115959+0000", @"1970-12-31T11:59:59Z" };
            yield return new object[] { @"19701231115959-0000", @"1970-12-31T11:59:59Z" };
            yield return new object[] { @"19701231115959.1234-0000", @"1970-12-31T11:59:59.1234Z" };
            yield return new object[] { @"19701231115959.000-0000", @"1970-12-31T11:59:59.000Z" };

            // If no time zone provided, it is treated as local
            // yield return new object[] { @"20050110045253", @"2005-01-10T04:52:53+08:00" };
            // yield return new object[] { @"20110103143428", @"2011-01-03T14:34:28+08:00" };
        }

        public static IEnumerable<object[]> GetInvalidDataForAddHyphensDate()
        {
            yield return new object[] { @"20badInput" };
            yield return new object[] { @"2020-11" };
            yield return new object[] { @"20201" };
            yield return new object[] { @"2020060" };
            yield return new object[] { @"20201301" };
            yield return new object[] { @"20200134" };
            yield return new object[] { @"20200230" };
        }

        public static IEnumerable<object[]> GetInvalidDataForFormatAsDateTime()
        {
            yield return new object[] { @"20badInput" };
            yield return new object[] { @"2020-11" };
            yield return new object[] { @"20140130080051--0500" };
            yield return new object[] { @"2014.051-0500" };
            yield return new object[] { @"20140130080051123+0500" };
            yield return new object[] { @"20201" };
            yield return new object[] { @"2020060" };
            yield return new object[] { @"20201301" };
            yield return new object[] { @"20200134" };
            yield return new object[] { @"20200230" };
            yield return new object[] { @"2020010130" };
            yield return new object[] { @"202001011080" };
            yield return new object[] { @"20200101101080" };
            yield return new object[] { @"20200101101080.-123" };
        }

        public static IEnumerable<object[]> GetValidDataForFormatWidthAsPeriod()
        {
            // happy path
            yield return new object[] {
                @"<effectiveTime><low value=""20200202000000"" /><width value=""2"" unit=""secs"" /></effectiveTime>",
                @"<effectiveTime><low value=""20200202000000"" /><high value=""20200202000002"" /></effectiveTime>",
            };
            yield return new object[] {
                @"<effectiveTime><high value=""20200202000000"" /><width value=""2"" unit=""secs"" /></effectiveTime>",
                @"<effectiveTime><low value=""20200201235958"" /><high value=""20200202000000"" /></effectiveTime>",
            };
            yield return new object[] {
                @"<effectiveTime><low value=""20200202000000"" /><width value=""2"" unit=""mins"" /></effectiveTime>",
                @"<effectiveTime><low value=""20200202000000"" /><high value=""20200202000200"" /></effectiveTime>",
            };
            yield return new object[] {
                @"<effectiveTime><high value=""20200202000000"" /><width value=""2"" unit=""mins"" /></effectiveTime>",
                @"<effectiveTime><low value=""20200201235800"" /><high value=""20200202000000"" /></effectiveTime>",
            };
            yield return new object[] {
                @"<effectiveTime><low value=""20200202000000"" /><width value=""2"" unit=""h"" /></effectiveTime>",
                @"<effectiveTime><low value=""20200202000000"" /><high value=""20200202020000"" /></effectiveTime>",
            };
            yield return new object[] {
                @"<effectiveTime><high value=""20200202000000"" /><width value=""2"" unit=""h"" /></effectiveTime>",
                @"<effectiveTime><low value=""20200201220000"" /><high value=""20200202000000"" /></effectiveTime>",
            };
            yield return new object[] {
                @"<effectiveTime><low value=""20200202000000"" /><width value=""2"" unit=""d"" /></effectiveTime>",
                @"<effectiveTime><low value=""20200202000000"" /><high value=""20200204000000"" /></effectiveTime>",
            };
            yield return new object[] {
                @"<effectiveTime><high value=""20200202000000"" /><width value=""2"" unit=""d"" /></effectiveTime>",
                @"<effectiveTime><low value=""20200131000000"" /><high value=""20200202000000"" /></effectiveTime>",
            };
            yield return new object[] {
                @"<effectiveTime><low value=""20200202"" /><width value=""2"" unit=""days"" /></effectiveTime>",
                @"<effectiveTime><low value=""20200202"" /><high value=""20200204"" /></effectiveTime>",
            };
            yield return new object[] {
                @"<effectiveTime><high value=""20200202"" /><width value=""2"" unit=""days"" /></effectiveTime>",
                @"<effectiveTime><low value=""20200131"" /><high value=""20200202"" /></effectiveTime>",
            };
            yield return new object[] {
                @"<effectiveTime><low value=""20200202"" /><width value=""2"" unit=""mo"" /></effectiveTime>",
                @"<effectiveTime><low value=""20200202"" /><high value=""20200402"" /></effectiveTime>",
            };
            yield return new object[] {
                @"<effectiveTime><high value=""20200202"" /><width value=""2"" unit=""mo"" /></effectiveTime>",
                @"<effectiveTime><low value=""20191202"" /><high value=""20200202"" /></effectiveTime>",
            };
            yield return new object[] {
                @"<effectiveTime><low value=""20200202"" /><width value=""2"" unit=""y"" /></effectiveTime>",
                @"<effectiveTime><low value=""20200202"" /><high value=""20220202"" /></effectiveTime>",
            };
            yield return new object[] {
                @"<effectiveTime><high value=""20200202"" /><width value=""2"" unit=""y"" /></effectiveTime>",
                @"<effectiveTime><low value=""20180202"" /><high value=""20200202"" /></effectiveTime>",
            };
            // garbage in, garbage out
            yield return new object[] {
                @"<effectiveTime><high value=""20200202"" /></effectiveTime>",
                @"<effectiveTime><high value=""20200202"" /></effectiveTime>",
            };
            yield return new object[] {
                @"<effectiveTime><width value=""2"" unit=""y"" /></effectiveTime>",
                @"<effectiveTime><width value=""2"" unit=""y"" /></effectiveTime>",
            };
            yield return new object[] {
                @"<effectiveTime><low value=""20200202"" /></effectiveTime>",
                @"<effectiveTime><low value=""20200202"" /></effectiveTime>",
            };
            yield return new object[] {
                @"<effectiveTime value=""20200202""></effectiveTime>",
                @"<effectiveTime value=""20200202""></effectiveTime>",
            };
        }

        public static IEnumerable<object[]> GetInvalidDataForFormatWidthAsPeriod()
        {
            yield return new object[] {
                @"<effectiveTime><low value=""20200202000000"" /><width value=""2"" unit=""nope"" /></effectiveTime>"
            };
            yield return new object[] {
                @"<effectiveTime><low value=""20200202000000"" /><width value=""2"" /></effectiveTime>"
            };
            yield return new object[] {
                @"<effectiveTime><low value=""20200202000000"" /><width unit=""s"" /></effectiveTime>"
            };
        }

        [Theory]
        [MemberData(nameof(GetValidDataForAddHyphensDate))]
        public void GivenADate_WhenAddHyphensDate_ConvertedDateShouldBeReturned(FluidValue input, string timeZoneHandling, FluidValue expected)
        {
            var context = new TemplateContext();
            var result = Filters.AddHyphensDate(input, new FilterArguments(StringValue.Create(timeZoneHandling)), context).Result;
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(GetValidDataWithoutTimeZoneForAddHyphensDateWithUtcTimeZoneHandling))]
        public void GivenAValidDataWithoutTimeZone_WhenAddHyphensDate_CorrectDateTimeShouldBeReturned(string input, string timeZoneHandling, DateTime inputDateTime)
        {
            var context = new TemplateContext();
            var result = Filters.AddHyphensDate(StringValue.Create(input), new FilterArguments(StringValue.Create(timeZoneHandling)), context).Result.ToStringValue();
            var dateTimeOffset = new DateTimeOffset(inputDateTime);
            var dateTimeString = dateTimeOffset.ToUniversalTime().ToString("yyyy-MM-dd");
            Assert.Contains(result, dateTimeString);
        }

        [Theory]
        [MemberData(nameof(GetValidDataForAddHyphensDateWithDefaultTimeZoneHandling))]
        public void GivenAValidData_WhenAddHyphensDateWithDefaultTimeZoneHandling_ConvertedDateTimeShouldBeReturned(FluidValue input, FluidValue expectedDateTime)
        {
            var context = new TemplateContext();
            var result = Filters.AddHyphensDate(input, new FilterArguments(), context).Result;
            Assert.Equal(expectedDateTime, result);
        }

        [Theory]
        [MemberData(nameof(GetValidDataForFormatAsDateTime))]
        public void GivenADateTime_WhenFormatAsDateTime_ConvertedDateTimeStringShouldBeReturned( FluidValue input, string timeZoneHandling, FluidValue expected)
        {
            var context = new TemplateContext();
            var result = Filters.FormatAsDateTime(input, new FilterArguments(StringValue.Create(timeZoneHandling)), context).Result;
            Assert.Equal(expected, result);
        }

        [Theory]
        [MemberData(nameof(GetValidDataWithoutTimeZoneForFormatAsDateTimeWithUtcTimeZoneHandling))]
        public void GivenAValidDataWithoutTimeZone_WhenFormatAsDateTime_ConvertedDateTimeShouldBeReturned(string input, string timeZoneHandling, string expectedDateTime, DateTime inputDateTime)
        {
            var context = new TemplateContext();
            var result = Filters.FormatAsDateTime(StringValue.Create(input), new FilterArguments(StringValue.Create(timeZoneHandling)), context).Result.ToStringValue();
            var dateTimeOffset = DateTimeOffset.Parse(result);
            dateTimeOffset = dateTimeOffset.AddHours(TimeZoneInfo.Local.GetUtcOffset(inputDateTime).TotalHours - 8);
            var dateTimeString = dateTimeOffset.ToString("yyyy-MM-ddTHH:mm:ssZ");
            Assert.Contains(expectedDateTime, dateTimeString);
        }

        [Theory]
        [MemberData(nameof(GetValidDataForFormatAsDateTimeWithDefaultTimeZoneHandling))]
        public void GivenAValidData_WhenFormatAsDateTimeWithDefaultTimeZoneHandling_ConvertedDateTimeShouldBeReturned(string input, string expectedDateTime)
        {
            var context = new TemplateContext();
            var result = Filters.FormatAsDateTime(StringValue.Create(input), new FilterArguments(), context).Result.ToStringValue();
            Assert.Equal(expectedDateTime, result);
        }

        [Theory]
        [MemberData(nameof(GetInvalidDataForAddHyphensDate))]
        public void GivenAnInvalidDateTime_WhenAddHyphensDate_ExceptionShouldBeThrown(string input)
        {
            var context = new TemplateContext();
            var exception = Assert.Throws<RenderException>(() => Filters.AddHyphensDate(StringValue.Create(input), new FilterArguments(), context));
            Assert.Equal(FhirConverterErrorCode.InvalidDateTimeFormat, exception.FhirConverterErrorCode);
        }

        [Theory]
        [MemberData(nameof(GetInvalidDataForFormatAsDateTime))]
        public void GivenAnInvalidDateTime_WhenFormatAsDateTime_ExceptionShouldBeThrown(string input)
        {
            var context = new TemplateContext();
            var exception = Assert.Throws<RenderException>(() => Filters.FormatAsDateTime(StringValue.Create(input), new FilterArguments(), context));
            Assert.Equal(FhirConverterErrorCode.InvalidDateTimeFormat, exception.FhirConverterErrorCode);
        }

        [Fact]
        public void GivenAnInvalidTimeZoneHandling_WhenFormatAsDateTime_ExceptionShouldBeThrown()
        {
            var context = new TemplateContext();
            var input = StringValue.Create("19701231115959+0600");
            var timeZoneHandling = StringValue.Create("abc");
            var exception = Assert.Throws<RenderException>(() => Filters.FormatAsDateTime(input, new FilterArguments(timeZoneHandling), context));
            Assert.Equal(FhirConverterErrorCode.InvalidTimeZoneHandling, exception.FhirConverterErrorCode);
        }

        [Fact]
        public void NowTest()
        {
            var context = new TemplateContext();
            // FHIR DateTime format
            var dateTime = DateTime.Parse(Filters.Now(StringValue.Create(string.Empty), FilterArguments.Empty, context).Result.ToStringValue());
            Assert.True(dateTime.Year > 2020);
            Assert.True(dateTime.Month >= 1 && dateTime.Month < 13);
            Assert.True(dateTime.Day >= 1 && dateTime.Day < 32);

            // Standard DateTime format, "d" stands for short day pattern
            var nowWithStandardFormat = Filters.Now(StringValue.Create(string.Empty), new FilterArguments(StringValue.Create("d")), context).Result.ToStringValue();
            Assert.Contains("/", nowWithStandardFormat);

            // Customized DateTime format
            var days = new List<string> { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
            var nowWithCustomizedFormat = Filters.Now(StringValue.Create(string.Empty), new FilterArguments(StringValue.Create("dddd, dd MMMM yyyy HH:mm:ss")), context).Result.ToStringValue();
            Assert.Contains(days, day => nowWithCustomizedFormat.StartsWith(day));

            // Null and empty format will lead to default format, which is short day with long time
            dateTime = DateTime.Parse(Filters.Now(StringValue.Create(string.Empty), new FilterArguments(NilValue.Instance), context).Result.ToStringValue());
            Assert.True(dateTime.Year > 2020);
            Assert.True(dateTime.Month >= 1 && dateTime.Month < 13);
            Assert.True(dateTime.Day >= 1 && dateTime.Day < 32);
            dateTime = DateTime.Parse(Filters.Now(StringValue.Create(string.Empty), new FilterArguments(StringValue.Create(string.Empty)), context).Result.ToStringValue());
            Assert.True(dateTime.Year > 2020);
            Assert.True(dateTime.Month >= 1 && dateTime.Month < 13);
            Assert.True(dateTime.Day >= 1 && dateTime.Day < 32);

            // Invalid DateTime format
            Assert.Throws<FormatException>(() => Filters.Now(StringValue.Create(string.Empty), new FilterArguments(StringValue.Create("a")), context));
        }

        [Theory]
        [MemberData(nameof(GetValidDataForFormatWidthAsPeriod))]
        public void FormatWidthAsPeriod_Valid(string inputStr, string expectedStr)
        {
            var context = new TemplateContext();
            var inputParsed = new CcdaDataParser().Parse(inputStr) as IDictionary<string, object>;
            var expectedParsed = new CcdaDataParser().Parse(expectedStr) as IDictionary<string, object>;
            var expected = expectedParsed["effectiveTime"] as Dictionary<string, object>;
            var actual = Filters.FormatWidthAsPeriod(DictionaryValue.Create(inputParsed["effectiveTime"], new TemplateOptions()), FilterArguments.Empty, context).Result as DictionaryValue;

            if (expected.ContainsKey("low")) {
                Assert.Equal((expected["low"] as Dictionary<string, object>)["value"], ((actual.GetValueAsync("low", context).Result as DictionaryValue).GetValueAsync("value", context)).Result.ToStringValue());
            }
            if (expected.ContainsKey("high")) {
                Assert.Equal((expected["high"] as Dictionary<string, object>)["value"], ((actual.GetValueAsync("high", context).Result as DictionaryValue).GetValueAsync("value", context)).Result.ToStringValue());
            }
            if (expected.ContainsKey("value")) {
                Assert.Equal(expected["value"], (actual.GetValueAsync("value", context).Result.ToStringValue()));
            }
        }

        [Theory]
        [MemberData(nameof(GetInvalidDataForFormatWidthAsPeriod))]
        public void FormatWidthAsPeriod_Invalid(string inputStr)
        {
            var context = new TemplateContext();
            var inputParsed = new CcdaDataParser().Parse(inputStr) as IDictionary<string, object>;
            var exception = Assert.Throws<RenderException>(() => Filters.FormatWidthAsPeriod(DictionaryValue.Create(inputParsed["effectiveTime"], new TemplateOptions()), FilterArguments.Empty, context));
            Assert.Equal(FhirConverterErrorCode.InvalidDateTimeFormat, exception.FhirConverterErrorCode);
        }
    }
}
