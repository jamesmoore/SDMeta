using Coderanger.ImageInfo.Decoders.Metadata;
using Coderanger.ImageInfo;
using NLog;
using System;
using System.Diagnostics;
using System.IO.Abstractions;
using System.IO;
using System.Linq;

namespace SDMetaTool
{
    public class PngFileLoader : IPngFileLoader
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly IFileSystem fileSystem;

        public PngFileLoader(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        public PngFile GetPngFile(string filename)
        {
            logger.Info($"Indexing: {filename}");

            try
            {
                return ReadPngFile(fileSystem, filename);
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Exception reading file {filename}");
                return null;
            }
        }

        public PngFile ReadPngFile(IFileSystem fileSystem, string filename)
        {
            var fileInfo = fileSystem.FileInfo.FromFileName(filename);

            var pngfile = new PngFile()
            {
                LastUpdated = fileInfo.LastWriteTime,
                Filename = fileInfo.FullName,
            };

            using (var stream = fileSystem.File.OpenRead(filename))
            {
                var imageInfo = ImageInfo.Get(stream);

                if (imageInfo.Metadata?.TryGetValue(MetadataProfileType.PngText, out var tags) ?? false && tags is not null)
                {
                    foreach (var tag in tags.Where(t => t is not null && t.HasValue))
                    {
                        if (tag.TryGetValue(out var metadataValue) && metadataValue is not null)
                        {
                            if (metadataValue.TagName == "parameters")
                            {
                                pngfile.Parameters = metadataValue.Value.ToString();
                            }
                            Debug.WriteLine($"{metadataValue.TagName} = {metadataValue.Value}");
                        }
                    }
                }
            }
            return pngfile;
        }
    }
}
