using SDMetaTool.Cache;
using System.Linq;

namespace SDMetaTool.Processors
{
	public class Rescan : IPngFileListProcessor
	{
		private readonly IPngFileDataSource pngFileDataSource;
		private readonly IDirectoryProcessor directoryProcessor;
		private readonly IPngFileLoader pngFileLoader;

		public Rescan(
			IDirectoryProcessor directoryProcessor,
			IPngFileDataSource pngFileDataSource,
			IPngFileLoader pngFileLoader)
		{
			this.pngFileDataSource = pngFileDataSource;
			this.directoryProcessor = directoryProcessor;
			this.pngFileLoader = pngFileLoader;
		}

		public void ProcessPngFiles(string root)
		{
			var fileNames = directoryProcessor.GetList(root);

			pngFileDataSource.ClearAll();

			var pngFiles = fileNames.Select(p => pngFileLoader.GetPngFile(p)).Where(p => p != null).ToList();

			foreach (var file in pngFiles)
			{
				file.Exists = true;
			}
		}
	}
}
