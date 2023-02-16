using NLog;
using SDMetaTool.Cache;
using System.Linq;

namespace SDMetaTool.Processors
{
	public class Rescan : IPngFileListProcessor
	{
		private readonly IPngFileDataSource pngFileDataSource;
		private readonly IFileLister fileLister;
		private readonly IPngFileLoader pngFileLoader;
		private static readonly Logger logger = LogManager.GetCurrentClassLogger();

		public Rescan(
			IFileLister fileLister,
			IPngFileDataSource pngFileDataSource,
			IPngFileLoader pngFileLoader)
		{
			this.pngFileDataSource = pngFileDataSource;
			this.fileLister = fileLister;
			this.pngFileLoader = pngFileLoader;
		}

		public void ProcessPngFiles(string root)
		{
			logger.Info("Rescan started");
			pngFileDataSource.BeginTransaction();
			var fileNames = fileLister.GetList(root);

			var knownExisting = pngFileDataSource.GetAllFilenames();

			var deleted = knownExisting.Except(fileNames);

			foreach (var file in deleted)
			{
				var fileToDelete = pngFileDataSource.ReadPngFile(file);
				fileToDelete.Exists = false;
				pngFileDataSource.WritePngFile(fileToDelete);
				logger.Info("Removing " + file);
			}

			var newFiles = fileNames.Except(knownExisting).Select(p => pngFileLoader.GetPngFile(p)).Where(p => p != null).ToList(); ;
			foreach (var file in newFiles.Where(p => p.Exists == false))
			{
				file.Exists = true;
				pngFileDataSource.WritePngFile(file);
				logger.Info("Adding " + file.FileName);
			}
			pngFileDataSource.CommitTransaction();
			logger.Info("Rescan finished");
		}
	}
}
