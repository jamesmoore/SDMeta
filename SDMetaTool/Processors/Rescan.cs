using NLog;
using SDMetaTool.Cache;
using System.Linq;
using System.Threading.Tasks;

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

		public async Task ProcessPngFiles(string root)
		{
			logger.Info("Rescan started");
			await pngFileDataSource.BeginTransaction();
			var fileNames = fileLister.GetList(root);

			var knownFiles = await pngFileDataSource.GetAll();

			var deleted = knownFiles.Where(p => p.Exists).Select(p => p.FileName).Except(fileNames);

			var knownFilesLookup = knownFiles.ToLookup(p => p.FileName);
			foreach (var file in deleted)
			{
				var fileToDelete = knownFilesLookup[file].Single();
				fileToDelete.Exists = false;
				await pngFileDataSource.WritePngFile(fileToDelete);
				logger.Info("Removing " + file);
			}

			var newFileNames = fileNames.Except(knownFiles.Where(p => p.Exists).Select(p => p.FileName));
			var newFileTasks = newFileNames.Select( p => pngFileLoader.GetPngFile(p)).ToList();

			await Task.WhenAll(newFileTasks.ToArray());

			foreach (var file in newFileTasks.Select(p => p.Result).Where(p => p != null && p.Exists == false))
			{
				file.Exists = true;
				await pngFileDataSource.WritePngFile(file);
				logger.Info("Adding " + file.FileName);
			}
			await pngFileDataSource.CommitTransaction();
			logger.Info("Rescan finished");
		}
	}
}
