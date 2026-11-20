using System;
using System.Collections.Generic;
using System.IO;
using Dibbs.Fhir.Liquid.Converter.FileSystems;
using Dibbs.Fhir.Liquid.Converter.Processors;
using Fluid;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests.FileSystems
{
    public class CachingFileProviderTests : IDisposable
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        private readonly PhysicalFileProvider _physical;
        private readonly CountingFileProvider _inner;
        private readonly CachingFileProvider _files;

        public CachingFileProviderTests()
        {
            Directory.CreateDirectory(_directory);
            _physical = new PhysicalFileProvider(_directory);
            _inner = new CountingFileProvider(_physical);
            _files = new CachingFileProvider(_inner);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void RepeatedLookupsCacheExistingAndMissingFiles(bool exists)
        {
            if (exists)
            {
                File.WriteAllText(Path.Combine(_directory, "part.liquid"), "content");
            }

            var first = _files.GetFileInfo("part.liquid");
            Assert.Equal(exists, first.Exists);
            Assert.Same(first, _files.GetFileInfo("part.liquid"));
            Assert.Equal(1, _inner.Calls);
        }

        [Fact]
        public void ConversionsReuseMetadataWhileKeepingTheirDataSeparate()
        {
            var path = Path.Combine(_directory, "part.liquid");
            File.WriteAllText(path, "\"fixed\"");
            var root = new FluidParser().Parse("{\"a\": {% include 'part' %}, \"b\": {% include 'part' %}, \"value\": \"{{ msg.ClinicalDocument.value._ }}\"}");
            var templates = new TemplateProvider(new List<Dictionary<string, IFluidTemplate>>
            {
                new Dictionary<string, IFluidTemplate> { { "Root", root } },
            });
            var processor = new CcdaProcessor(NullLogger<CcdaProcessor>.Instance, new TemplateOptions());

            foreach (var value in new[] { "first", "second" })
            {
                var input = $"<ClinicalDocument><value>{value}</value></ClinicalDocument>";
                var result = JObject.Parse(processor.Convert(input, "Root", _directory, templates, _files));
                Assert.Equal("fixed", result["a"]?.Value<string>());
                Assert.Equal("fixed", result["b"]?.Value<string>());
                Assert.Equal(value, result["value"]?.Value<string>());
                // The extensionless miss and .liquid hit are reused across conversions.
                Assert.Equal(2, _inner.Calls);
            }
        }

        public void Dispose()
        {
            _physical.Dispose();
            Directory.Delete(_directory, recursive: true);
        }

        private sealed class CountingFileProvider(IFileProvider inner) : IFileProvider
        {
            public int Calls { get; private set; }

            public IFileInfo GetFileInfo(string subpath)
            {
                Calls++;
                return inner.GetFileInfo(subpath);
            }

            public IDirectoryContents GetDirectoryContents(string subpath) => inner.GetDirectoryContents(subpath);
            public IChangeToken Watch(string filter) => inner.Watch(filter);
        }
    }
}
