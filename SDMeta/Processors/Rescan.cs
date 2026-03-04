using Microsoft.Extensions.Logging;
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
        IImageFileDataSource imageFileDataSource,
        IImageFileLoader imageFileLoader,
        ILogger<Rescan> logger
        ) : IImageFileListProcessor
    {
        public event EventHandler<float>? ProgressNotification;

        public async Task ProcessImageFiles()
        {
            logger.LogInformation("Rescan started");
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var fileNames = imageDir.GetPath().Select(fileLister.GetList).SelectMany(p => p).Distinct().ToList();

            var knownExisting = imageFileDataSource.GetAllFilenames();

            var deleted = knownExisting.Except(fileNames).ToList();
            var added = fileNames.Except(knownExisting).ToList();

            await PartialRescan(added, deleted);
            logger.LogInformation("Rescan finished in {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);
        }

        public async Task PartialRescan(IEnumerable<string> added, IEnumerable<string> deleted)
        {
            var total = added.Count() + deleted.Count();
            if (total > 0)
            {
                var steps = total <= 100 ? 1 : total / 100;
                var multiplier = (float)(total <= 100 ? 100.0 / total : 1);

                int position = 0;

                imageFileDataSource.BeginTransaction();

                foreach (var file in deleted)
                {
                    var fileToDelete = imageFileDataSource.ReadImageFile(file);
                    fileToDelete.Exists = false;
                    imageFileDataSource.WriteImageFile(fileToDelete);
                    logger.LogInformation("Removing {file}", file);
                    Notify(steps, multiplier, ++position);
                }

                var chunkedTasks = added.Select(GetPngFile).Chunk(100);

                foreach (var chunk in chunkedTasks)
                {
                    await Task.WhenAll(chunk);
                    imageFileDataSource.CommitTransaction();
                    imageFileDataSource.BeginTransaction();
                    position += chunk.Count();
                    ProgressNotification?.Invoke(this, multiplier * position / steps);
                }
                imageFileDataSource.CommitTransaction();
                imageFileDataSource.PostUpdateProcessing();
            }
        }

        private async Task GetPngFile(string addedFile)
        {
            _ = await imageFileLoader.GetImageFile(addedFile);
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
