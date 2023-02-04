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
			var fileNames = fileLister.GetList(root);

			var knownFiles = pngFileDataSource.GetAll();

			var deleted = knownFiles.Select(p => p.Filename).Except(fileNames);

			var knownFilesLookup = knownFiles.ToLookup(p => p.Filename);
			foreach (var file in deleted)
			{
				var fileToDelete = knownFilesLookup[file].Single();
				fileToDelete.Exists = false;
				pngFileDataSource.WritePngFile(fileToDelete);
			}

			var newFiles = fileNames.Except(knownFiles.Select(p => p.Filename)).Select(p => pngFileLoader.GetPngFile(p)).Where(p => p != null).ToList(); ;
			foreach(var file in newFiles)
			{
				pngFileDataSource.WritePngFile(file);
			}
		}
	}
}
