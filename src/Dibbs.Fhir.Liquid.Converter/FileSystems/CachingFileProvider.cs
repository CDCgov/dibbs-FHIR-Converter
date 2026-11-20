using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Dibbs.Fhir.Liquid.Converter.FileSystems
{
    /// <summary>
    /// Caches metadata, including missing-file lookups, for this provider's lifetime.
    /// Template files remain unchanged throughout the deployment so we do not need to worry about invalidating cache entries.
    /// </summary>
    public sealed class CachingFileProvider : IFileProvider
    {
        private readonly IFileProvider _inner;
        private readonly ConcurrentDictionary<string, IFileInfo> _files = new ConcurrentDictionary<string, IFileInfo>(StringComparer.Ordinal);

        public CachingFileProvider(IFileProvider inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            return _files.GetOrAdd(subpath, (path, provider) => provider.GetFileInfo(path), _inner);
        }

        public IDirectoryContents GetDirectoryContents(string subpath) => _inner.GetDirectoryContents(subpath);

        // Required by IFileProvider; deployed templates do not change.
        public IChangeToken Watch(string filter) => NullChangeToken.Singleton;
    }
}
