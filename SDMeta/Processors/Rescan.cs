using NLog;
using SDMeta.Cache;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SDMeta.Processors
{
	public class Rescan(
		IImageDir imageDir,
		IFileLister fileLister,
		IPngFileDataSource pngFileDataSource,
		IPngFileLoader pngFileLoader) : IPngFileListProcessor
	{
		private static readonly Logger logger = LogManager.GetCurrentClassLogger();
		public event EventHandler<float>? ProgressNotification;

		public async Task ProcessPngFiles()
        {
            logger.Info("Rescan started");
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var fileNames = imageDir.GetPath().Select(fileLister.GetList).SelectMany(p => p).Distinct().ToList();

            var knownExisting = pngFileDataSource.GetAllFilenames();

            var deleted = knownExisting.Except(fileNames).ToList();
            var added = fileNames.Except(knownExisting).ToList();

            await PartialRescan(added, deleted);
            logger.Info($"Rescan finished in {stopwatch.ElapsedMilliseconds}ms");
        }

        public async Task PartialRescan(IEnumerable<string> added, IEnumerable<string> deleted)
        {
            var total = added.Count() + deleted.Count();
            if (total > 0)
            {
                var steps = total <= 100 ? 1 : total / 100;
                var multiplier = (float)(total <= 100 ? 100.0 / total : 1);

                int position = 0;

                pngFileDataSource.BeginTransaction();

                foreach (var file in deleted)
                {
                    var fileToDelete = pngFileDataSource.ReadPngFile(file);
                    fileToDelete.Exists = false;
                    pngFileDataSource.WritePngFile(fileToDelete);
                    logger.Info("Removing " + file);
                    Notify(steps, multiplier, ++position);
                }

                var chunkedTasks = added.Select(GetPngFile).Chunk(100);

                foreach (var chunk in chunkedTasks)
                {
                    await Task.WhenAll(chunk);
                    pngFileDataSource.CommitTransaction();
                    pngFileDataSource.BeginTransaction();
                    position += chunk.Count();
                    ProgressNotification?.Invoke(this, multiplier * position / steps);
                }
                pngFileDataSource.CommitTransaction();
                pngFileDataSource.PostUpdateProcessing();
            }
        }

        private async Task GetPngFile(string addedFile)
        {
            _ = await pngFileLoader.GetPngFile(addedFile);
        }

        private void Notify(int steps, float multiplier, int position)
		{
			if (position % steps == 0)
			{
                ProgressNotification?.Invoke(this, multiplier * position / steps);
			}
		}
	}
}
