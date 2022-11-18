using Coderanger.ImageInfo;
using Coderanger.ImageInfo.Decoders.Metadata;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;

namespace SDMetaTool
{
    public class PngFile
    {
        private readonly IFileSystem fileSystem;

        public PngFile(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        public PngFile(IFileSystem fileSystem, string filename) : this(fileSystem)
        {
            var fileInfo = fileSystem.FileInfo.FromFileName(filename);

            using (var stream = File.OpenRead(filename))
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
                                Parameters = metadataValue.Value.ToString();
                            }
                            Debug.WriteLine($"{metadataValue.TagName} = {metadataValue.Value}");
                        }
                    }
                }
            }

            LastUpdated = fileInfo.LastWriteTime;
            Filename = fileInfo.FullName;
        }

        public string Filename { get; set; }
        public DateTime LastUpdated { get; set; }
        public string Parameters { get; set; }
    }
}
