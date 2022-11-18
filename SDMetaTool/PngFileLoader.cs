using NLog;
using System;
using System.IO.Abstractions;
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
                return BuildPngFile(filename);
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Exception reading file {filename}");
                return null;
            }
        }

        private PngFile BuildPngFile(string filename)
        {
            var track = new PngFile(fileSystem, filename);
            return track;
        }
    }
}
