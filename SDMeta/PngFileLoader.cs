using NLog;
using SDMeta.Metadata;
using System;
using System.IO.Abstractions;
using System.Linq;

namespace SDMeta
{
    public class PngFileLoader(IFileSystem fileSystem) : IPngFileLoader
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

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

        private PngFile ReadPngFile(IFileSystem fileSystem, string filename)
        {
            var fileInfo = fileSystem.FileInfo.New(filename);

            var prompt = ExtractPromptFromPngText(fileSystem, filename);

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

        private static (PromptFormat promptFormat, string prompt) ExtractPromptFromPngText(IFileSystem fileSystem, string filename)
        {
            var metadata = PngMetadataExtractor.ExtractTextualInformation(filename);

            var promptMetadata = metadata.FirstOrDefault(p => p.Key == "parameters" || p.Key == "prompt");

            switch (promptMetadata.Key)
            {
                case "parameters":
                    return (PromptFormat.Auto1111, promptMetadata.Value);
                case "prompt":
                    return (PromptFormat.ComfyUI, promptMetadata.Value);
                default:
                    return (PromptFormat.None, null);
            }
        }
    }
}
