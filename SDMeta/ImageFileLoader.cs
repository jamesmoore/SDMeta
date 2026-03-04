using Microsoft.Extensions.Logging;
using SDMeta.Metadata;
using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;

namespace SDMeta
{
    public class ImageFileLoader(IFileSystem fileSystem, ILogger<ImageFileLoader> logger) : IImageFileLoader
    {
        public async Task<ImageFile> GetImageFile(string filename)
        {
            logger.LogInformation("Indexing: {filename}", filename);

            try
            {
                return await ReadImageFile(fileSystem, filename);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception reading file {filename}", filename);
                throw;
            }
        }

        private static async Task<ImageFile> ReadImageFile(IFileSystem fileSystem, string filename)
        {
            var fileInfo = fileSystem.FileInfo.New(filename);

            var prompt = await ExtractPromptFromPngText(fileSystem, filename);

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

        private async static Task<(PromptFormat promptFormat, string prompt)> ExtractPromptFromPngText(IFileSystem fileSystem, string filename)
        {
            using var fs = fileSystem.FileStream.New(filename, FileMode.Open, FileAccess.Read);

            var metadata = 
                filename.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? PngMetadataExtractor.ExtractTextualInformation(fs) :
                filename.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ? JpegMetadataExtractor.ExtractTextualInformation(fs) : 
                null;

            var promptMetadata = await metadata.FirstOrDefaultAsync(p => p.Key == "parameters" || p.Key== "prompt" || p.Key == "UserComment");

            return promptMetadata.Key switch
            {
                "parameters" => (PromptFormat.Auto1111, promptMetadata.Value),
                "prompt" => (PromptFormat.ComfyUI, promptMetadata.Value),
                "UserComment" => (PromptFormat.Auto1111, promptMetadata.Value),
                _ => (PromptFormat.None, null),
            };
        }
    }
}
