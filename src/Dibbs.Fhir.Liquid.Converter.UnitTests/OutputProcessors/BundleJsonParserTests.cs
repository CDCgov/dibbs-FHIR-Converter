using System.Linq;
using Dibbs.Fhir.Liquid.Converter.Exceptions;
using Dibbs.Fhir.Liquid.Converter.Models;
using Dibbs.Fhir.Liquid.Converter.OutputProcessors;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests.OutputProcessors
{
    public class BundleJsonParserTests
    {
        [Theory]
        [InlineData("{}", "{}")]
        [InlineData("{,,,}", "{}")]
        [InlineData("""{"entry":[]}""", "{}")]
        [InlineData("""{"entry":[,{},[],"",,]}""", "{}")]
        [InlineData("""{,"type":"document",,"entry":[,{"id":"one",},,{"id":"two"},],"total":2,}""",
                    """{"type":"document","entry":[{"id":"one"},{"id":"two"}],"total":2}""")]
        [InlineData("""{"entry":[null,false,true,0,-1,[],[{},2],{"a":"","b":null}]}""",
                    """{"entry":[null,false,true,0,-1,[2],{"b":null}]}""")]
        [InlineData("""{"entry":[{"id":"one"}],"entry":[{},"",[]]}""", """{"entry":[{"id":"one"}]}""")]
        [InlineData("""{"entry":[{"id":"one"}],"entry":[{"id":"two"}]}""", """{"entry":[{"id":"two"}]}""")]
        [InlineData("""{"entry":null}""", """{"entry":null}""")]
        [InlineData("""{"entry":{"id":"one"}}""", """{"entry":{"id":"one"}}""")]
        [InlineData("""{"\u0065ntry":[{"id":"one"}]}""", """{"entry":[{"id":"one"}]}""")]
        [InlineData("""{"meta":{"entry":[,{"nested":[1,,2,]},]},"entry":[{"text":"a \"}] , \"entry\": [","path":"C:\\folder\\file"}]}""",
                    """{"meta":{"entry":[{"nested":[1,2]}]},"entry":[{"text":"a \"}] , \"entry\": [","path":"C:\\folder\\file"}]}""")]
        [InlineData("""{"entry":[{"a":1,"b":2,"a":3,"b":""}]}""", """{"entry":[{"b":2,"a":3}]}""")]
        [InlineData("""{"entry":[{"id":"one"}],"meta":{"x":1},"entry":[{"id":"two"}]}""",
                    """{"meta":{"x":1},"entry":[{"id":"two"}]}""")]
        [InlineData("""{"a":1,"b":2,"a":3}""", """{"b":2,"a":3}""")]
        public void ParseJson_PreservesCleanupAndEntryBoundaries(string input, string expected)
        {
            Assert.Equal(expected, PostProcessor.ParseJson(input).ToString(Formatting.None));
        }

        [Theory]
        [InlineData("")]
        [InlineData("[]")]
        [InlineData("""{"entry":[{"a":1}{"b":2}]}""")]
        [InlineData("""{"entry":[{}true]}""")]
        [InlineData("""{"entry":[{"a":1 "b":2}]}""")]
        [InlineData("""{"entry":[{"a":[1 2]}]}""")]
        [InlineData("""{"entry":[{}],"total":}""")]
        [InlineData("""{"entry":[{}] "total":1}""")]
        [InlineData("""{"entry":[{"a":1}}}""")]
        [InlineData("""{"entry":[{"a":"unterminated}]}""")]
        [InlineData("""{"entry":[{"a":1}]""")]
        public void ParseJson_RejectsMalformedBundles(string input)
        {
            var exception = Assert.Throws<PostprocessException>(() => PostProcessor.ParseJson(input));
            Assert.Equal(FhirConverterErrorCode.JsonParsingError, exception.FhirConverterErrorCode);
        }

        [Fact]
        public void ParseJson_PreservesDateStringsAndNumericTypes()
        {
            var result = PostProcessor.ParseJson("""
                {"timestamp":"2026-01-02T03:04:05Z","entry":[
                    {"date":"2026-01-02T03:04:05Z","integer":9223372036854775808,"number":1.25e2}
                ]}
                """);

            Assert.Equal(JTokenType.String, result["timestamp"].Type);
            Assert.Equal(JTokenType.String, result["entry"][0]["date"].Type);
            Assert.Equal(JTokenType.Integer, result["entry"][0]["integer"].Type);
            Assert.Equal("9223372036854775808", result["entry"][0]["integer"].ToString());
            Assert.Equal(125.0, result["entry"][0]["number"].Value<double>());
        }

        [Fact]
        public void ParseJson_PreservesDocumentDepthLimit()
        {
            string Bundle(int depth) => "{\"entry\":[{\"a\":" + new string('[', depth) + "0" + new string(']', depth) + "}]}";

            Assert.NotNull(PostProcessor.ParseJson(Bundle(61)));
            Assert.Throws<JsonReaderException>(() => PostProcessor.ParseJson(Bundle(62)));
        }

        [Fact]
        public void MergeJson_ReusesEntriesAndMergesWithoutReplacingTheOriginalResource()
        {
            var bundle = JObject.Parse("""
                {"entry":[
                    {"resource":{"resourceType":"Patient","id":"p","name":["first"]}},
                    {"resource":{"resourceType":"Observation","id":"o"}},
                    {"resource":{"resourceType":"Patient","id":"p","name":["first","second"]}},
                    {"resource":{"resourceType":"Patient","id":"p","active":true}}
                ]}
                """);
            var patientEntry = bundle["entry"][0];
            var patient = patientEntry["resource"];
            var observationEntry = bundle["entry"][1];

            var result = PostProcessor.MergeJson(bundle);

            Assert.Same(bundle, result);
            Assert.Equal(2, result["entry"].Count());
            Assert.Same(patientEntry, result["entry"][0]);
            Assert.Same(patient, result["entry"][0]["resource"]);
            Assert.Same(observationEntry, result["entry"][1]);
            Assert.Equal(new[] { "first", "second" }, patient["name"].Values<string>());
            Assert.True(patient["active"].Value<bool>());
        }

        [Fact]
        public void Process_MergesNonAdjacentEntriesAfterCleaningAndPreservesMetadata()
        {
            var input = """
                {"resourceType":"Bundle","entry":[,
                    {"resource":{"resourceType":"Patient","id":"p","name":"first"}},
                    {"resource":{"resourceType":"Observation","id":"o"}},
                    {"resource":{"resourceType":"Patient","id":"p","name":"","active":true}},
                ],"total":2}
                """;
            var expected = JObject.Parse("""
                {"resourceType":"Bundle","entry":[
                    {"resource":{"resourceType":"Patient","id":"p","name":"first","active":true}},
                    {"resource":{"resourceType":"Observation","id":"o"}}
                ],"total":2}
                """);

            Assert.Equal(expected.ToString(Formatting.None), PostProcessor.Process(input).ToString(Formatting.None));
        }
    }
}
