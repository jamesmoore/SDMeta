﻿using NLog;
using SDMeta.Cache;
using System;
using System.Linq;

namespace SDMeta.Processors
{
	public class Rescan(
		IImageDir imageDir,
		IFileLister fileLister,
		IPngFileDataSource pngFileDataSource,
		IPngFileLoader pngFileLoader) : IPngFileListProcessor
	{
		private static readonly Logger logger = LogManager.GetCurrentClassLogger();
		public event EventHandler<float> ProgressNotification;

		public void ProcessPngFiles()
		{
			logger.Info("Rescan started");
			pngFileDataSource.BeginTransaction();
			var fileNames = imageDir.GetPath().Select(fileLister.GetList).SelectMany(p => p).Distinct().ToList();

			var knownExisting = pngFileDataSource.GetAllFilenames();

			var deleted = knownExisting.Except(fileNames).ToList();
			var added = fileNames.Except(knownExisting).ToList();

			var total = added.Count + deleted.Count;
			if (total > 0)
			{
				var steps = total <= 100 ? 1 : total / 100;
				var multiplier = (float)(total <= 100 ? 100.0 / total : 1);

				int position = 0;

				foreach (var file in deleted)
				{
					var fileToDelete = pngFileDataSource.ReadPngFile(file);
					fileToDelete.Exists = false;
					pngFileDataSource.WritePngFile(fileToDelete);
					logger.Info("Removing " + file);
					Notify(steps, multiplier, ++position);
				}

				foreach (var addedFile in added)
				{
					var file = pngFileLoader.GetPngFile(addedFile);
					if (file != null && file.Exists == false)
					{
						file.Exists = true;
						pngFileDataSource.WritePngFile(file);
						logger.Info("Adding " + file.FileName);
					}
					Notify(steps, multiplier, ++position);
				}
			}
			pngFileDataSource.CommitTransaction();
			pngFileDataSource.PostUpdateProcessing();
			logger.Info("Rescan finished");
		}

		private void Notify(int steps, float multiplier, int position)
		{
			if (position % steps == 0)
			{
				ProgressNotification?.Invoke(this, multiplier * position / steps);
				pngFileDataSource?.CommitTransaction();
				pngFileDataSource?.BeginTransaction();
			}
		}
	}
}
