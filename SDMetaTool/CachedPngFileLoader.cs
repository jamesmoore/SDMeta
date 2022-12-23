using NLog;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;

namespace SDMetaTool
{
    public class CachedPngFileLoader : IPngFileLoader, IDisposable
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private readonly IPngFileLoader inner;
        private readonly bool whatif;
        private readonly IFileSystem fileSystem;
        private readonly Dictionary<string, PngFile> cache;

        public string GetPath() => $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}{fileSystem.Path.DirectorySeparatorChar}SDMetaTool{fileSystem.Path.DirectorySeparatorChar}cache.json";

        public CachedPngFileLoader(IFileSystem fileSystem, IPngFileLoader inner, bool whatif)
        {
            this.inner = inner;
            this.whatif = whatif;
            this.fileSystem = fileSystem;

            var path = GetPath();
            if (fileSystem.File.Exists(path))
            {
                cache = new PngFileCache(fileSystem).ReadCache(path).ToDictionary(p => p.Filename, p => p);
            }
            else
            {
                logger.Debug("Initalising new cache");
                cache = new Dictionary<string, PngFile>();
            }
        }

        public void Dispose()
        {
            if (whatif == false)
            {
                Flush();
            }
        }

        public void Flush()
        {
            var path = GetPath();
            new PngFileCache(fileSystem).WriteCache(path, cache.Values);
        }

        public PngFile GetPngFile(string filename)
        {
            var fileInfo = fileSystem.FileInfo.New(filename);
            var realFileName = fileInfo.FullName;

            if (cache.TryGetValue(realFileName, out PngFile value) && value.LastUpdated == fileInfo.LastWriteTime)
            {
                return value;
            }
            else
            {
                var info = inner.GetPngFile(realFileName);
                if (info != null)
                {
                    cache[realFileName] = info;
                }
                return info;
            }
        }
    }
}
