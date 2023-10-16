using Coderanger.ImageInfo;
using Coderanger.ImageInfo.Decoders.Metadata;
using Coderanger.ImageInfo.Decoders.Metadata.Png;
using NLog;
using System;
using System.IO.Abstractions;
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
			var fileInfo = fileSystem.FileInfo.New(filename);

			var prompt = ExtractPromptFromPngText(fileSystem, filename);

			var pngfile = new PngFile()
			{
				LastUpdated = fileInfo.LastWriteTime,
				FileName = fileInfo.FullName,
				Length = fileInfo.Length,
			    PromptFormat = prompt.promptFormat,
				Prompt = prompt.prompt,
			};

			return pngfile;
		}

		private static (PromptFormat promptFormat, string prompt) ExtractPromptFromPngText(IFileSystem fileSystem, string filename)
		{
			using (var stream = fileSystem.File.OpenRead(filename))
			{
				var imageInfo = ImageInfo.Get(stream);

				if (imageInfo.Metadata?.TryGetValue(MetadataProfileType.PngText, out var tags) ?? false && tags is not null)
				{
					foreach (var tag in tags.Where(t => t is not null && t.HasValue))
					{
						if (tag.TryGetValue(out var metadataValue) && metadataValue is not null && metadataValue.Value is PngText)
						{
							var rawParameters = (metadataValue.Value as PngText).TextValue;
							if (metadataValue.TagName == "parameters")
							{
								return (PromptFormat.Auto1111, rawParameters);
							}
							else if (metadataValue.TagName == "prompt")
							{
								return (PromptFormat.ComfyUI, rawParameters);
							}
						}
					}
				}
			}
			return (PromptFormat.None, null);
		}
	}
}
