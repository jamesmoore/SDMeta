using NLog;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text.Json;

namespace SDMetaTool
{
    internal class PngFileCache
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private readonly IFileSystem fileSystem;

        public PngFileCache(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        public IEnumerable<PngFile> ReadCache(string path)
        {
            logger.Debug($"Reading cache at {path}");
            var cacheJson = fileSystem.File.ReadAllText(path);
            var deserialised = JsonSerializer.Deserialize<List<PngFileDTO>>(cacheJson);
            var dictionary = deserialised.Select(PngFileDTOToPngFile).ToList();
            return dictionary;
        }

        public void WriteCache(string path, IEnumerable<PngFile> cache)
        {
            logger.Debug($"Flushing cache to {path}");
            var serialized = JsonSerializer.Serialize(cache.Select(PngFileToPngFileDTO), new JsonSerializerOptions()
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            });

            string path1 = fileSystem.FileInfo.New(path).Directory.FullName;
            if (fileSystem.Directory.Exists(path1) == false)
            {
                fileSystem.Directory.CreateDirectory(path1);
            }
            fileSystem.File.WriteAllText(path, serialized);
        }

        private static PngFile PngFileDTOToPngFile(PngFileDTO trackDTO)
        {
            return new PngFile()
            {
                Filename = trackDTO.Filename,
                LastUpdated = trackDTO.LastUpdated,
                Parameters = trackDTO.Parameters,
                Length = trackDTO.Length,
            };
        }

        private static PngFileDTO PngFileToPngFileDTO(PngFile track)
        {
            return new PngFileDTO()
            {
                Filename = track.Filename,
                LastUpdated = track.LastUpdated,
                Parameters = track.Parameters,
                Length = track.Length,
            };
        }

        internal class PngFileDTO
        {
            public string Filename { get; set; }
            public DateTime LastUpdated { get; set; }
            public long Length { get; set; }
            public GenerationParams Parameters { get; set; }
        }
    }
}
