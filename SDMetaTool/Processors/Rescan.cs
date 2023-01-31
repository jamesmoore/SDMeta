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

			pngFileDataSource.ClearAll();

			var pngFiles = fileNames.Select(p => pngFileLoader.GetPngFile(p)).Where(p => p != null).ToList();

			foreach (var file in pngFiles)
			{
				file.Exists = true;
			}
		}
	}
}
