using NLog;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text.Json;

namespace SDMetaTool
{
    public class CachedPngFileLoader : IPngFileLoader, IDisposable
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private readonly IPngFileLoader inner;
        private readonly bool whatif;
        private readonly IFileSystem fileSystem;
        private readonly Dictionary<string, PngFile> cache;

        public string GetPath() => $"{fileSystem.Path.DirectorySeparatorChar}data{fileSystem.Path.DirectorySeparatorChar}cache.json";

        public CachedPngFileLoader(IFileSystem fileSystem, IPngFileLoader inner, bool whatif)
        {
            this.inner = inner;
            this.whatif = whatif;
            this.fileSystem = fileSystem;

            var path = GetPath();
            if (fileSystem.File.Exists(path))
            {
                logger.Debug($"Reading cache at {path}");
                var cacheJson = fileSystem.File.ReadAllText(path);
                var deserialised = JsonSerializer.Deserialize<List<PngFileDTO>>(cacheJson);

                var grouped = deserialised.GroupBy(p => p.Filename).Where(p => p.Count() == 1);
                var tracks = grouped.SelectMany(p => p);
                cache = tracks.ToDictionary(p => p.Filename, p => PngFileDTOToPngFile(p));
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
            logger.Debug($"Flushing cache to {path}");
            var serialized = JsonSerializer.Serialize(cache.Select(p => PngFileToPngFileDTO(p.Value)), new JsonSerializerOptions()
            {
                WriteIndented = true,
            });

            string path1 = fileSystem.FileInfo.FromFileName(path).Directory.FullName;
            if (fileSystem.Directory.Exists(path1) == false)
            {
                fileSystem.Directory.CreateDirectory(path1);
            }
            fileSystem.File.WriteAllText(path, serialized);
        }

        public PngFile GetPngFile(string filename)
        {
            var fileInfo = fileSystem.FileInfo.FromFileName(filename);

            if (cache.ContainsKey(filename) && cache[filename].LastUpdated == fileInfo.LastWriteTime)
            {
                return cache[filename];
            }
            else
            {
                var info = inner.GetPngFile(filename);
                if (info != null)
                {
                    cache[filename] = info;
                }
                return info;
            }
        }

        private PngFile PngFileDTOToPngFile(PngFileDTO trackDTO)
        {
            return new PngFile()
            {
                Filename = trackDTO.Filename,
                LastUpdated = trackDTO.LastUpdated,
                Parameters = trackDTO.Parameters,
            };
        }

        private PngFileDTO PngFileToPngFileDTO(PngFile track)
        {
            return new PngFileDTO()
            {
                Filename = track.Filename,
                LastUpdated = track.LastUpdated,
                Parameters = track.Parameters,
            };
        }
    }

    internal class PngFileDTO
    {
        public string Filename { get; set; }
        public DateTime LastUpdated { get; set; }
        public string Parameters { get; set; }
    }
}
