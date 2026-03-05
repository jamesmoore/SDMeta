using Microsoft.Extensions.Logging;
using SDMeta.Metadata;
using System;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;

namespace SDMeta
{
    public class ImageFileLoader(ILogger<ImageFileLoader> logger) : IImageFileLoader
    {
        public async Task<ImageFile> GetImageFile(IFileInfo fileInfo)
        {
            logger.LogInformation("Indexing: {filename}", fileInfo.FullName);

            try
            {
                return await ReadImageFile(fileInfo);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception reading file {filename}", fileInfo.FullName);
                throw;
            }
        }

        private async Task<ImageFile> ReadImageFile(IFileInfo fileInfo)
        {
            var prompt = await ExtractPromptFromPngText(fileInfo);

            var imagefile = new ImageFile(
                fileInfo.FullName,
                fileInfo.LastWriteTime,
                fileInfo.Length,
                prompt.promptFormat,
                prompt.prompt,
                true
            );

            return imagefile;
        }

        private async Task<(PromptFormat promptFormat, string? prompt)> ExtractPromptFromPngText(IFileInfo fileInfo)
        {
            var extension = fileInfo.Extension.ToLowerInvariant();
            using var fs = fileInfo.OpenRead();

            var metadata =
                extension == ".png" ? PngMetadataExtractor.ExtractTextualInformation(fs) :
                extension == ".jpg" ? JpegMetadataExtractor.ExtractTextualInformation(fs) :
                null;

            if (metadata != null)
            {
                var promptMetadata = await metadata.FirstOrDefaultAsync(p => p.Key == "parameters" || p.Key == "prompt" || p.Key == "UserComment");

                return promptMetadata.Key switch
                {
                    "parameters" => (PromptFormat.Auto1111, promptMetadata.Value),
                    "prompt" => (PromptFormat.ComfyUI, promptMetadata.Value),
                    "UserComment" => (PromptFormat.Auto1111, promptMetadata.Value),
                    _ => (PromptFormat.None, null),
                };
            }
            else
            {
                return (PromptFormat.None, null);
            }
        }
    }
}
