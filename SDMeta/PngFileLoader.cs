using Microsoft.Extensions.Logging;
using SDMeta.Metadata;
using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;

namespace SDMeta
{
    public class PngFileLoader(IFileSystem fileSystem, ILogger<PngFileLoader> logger) : IPngFileLoader
    {
        public async Task<PngFile> GetPngFile(string filename)
        {
            logger.LogInformation("Indexing: {filename}", filename);

            try
            {
                return await ReadPngFile(fileSystem, filename);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception reading file {filename}", filename);
                throw;
            }
        }

        private static async Task<PngFile> ReadPngFile(IFileSystem fileSystem, string filename)
        {
            var fileInfo = fileSystem.FileInfo.New(filename);

            var prompt = await ExtractPromptFromPngText(fileSystem, filename);

            var pngfile = new PngFile(
                fileInfo.FullName,
                fileInfo.LastWriteTime,
                fileInfo.Length,
                prompt.promptFormat,
                prompt.prompt,
                true
            );

            return pngfile;
        }

        private async static Task<(PromptFormat promptFormat, string prompt)> ExtractPromptFromPngText(IFileSystem fileSystem, string filename)
        {
            using var fs = fileSystem.FileStream.New(filename, FileMode.Open, FileAccess.Read);

            var metadata = PngMetadataExtractor.ExtractTextualInformation(fs);

            var promptMetadata = await metadata.FirstOrDefaultAsync(p => p.Key == "parameters" || p.Key== "prompt");

            return promptMetadata.Key switch
            {
                "parameters" => (PromptFormat.Auto1111, promptMetadata.Value),
                "prompt" => (PromptFormat.ComfyUI, promptMetadata.Value),
                _ => (PromptFormat.None, null),
            };
        }
    }
}
