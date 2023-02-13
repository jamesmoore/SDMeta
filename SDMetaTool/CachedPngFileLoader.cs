using SDMetaTool.Cache;
using System.IO.Abstractions;
using System.Threading.Tasks;

namespace SDMetaTool
{
	public class CachedPngFileLoader : IPngFileLoader
	{
		private readonly IPngFileLoader inner;
		private readonly IPngFileDataSource pngFileDataSource;
		private readonly IFileSystem fileSystem;

		public CachedPngFileLoader(
			IFileSystem fileSystem,
			IPngFileLoader inner,
			IPngFileDataSource pngFileDataSource)
		{
			this.inner = inner;
			this.pngFileDataSource = pngFileDataSource;
			this.fileSystem = fileSystem;
		}

		public async Task<PngFile> GetPngFile(string filename)
		{
			var fileInfo = fileSystem.FileInfo.New(filename);
			var pngFile = await pngFileDataSource.ReadPngFile(filename);
			if (pngFile != null && pngFile.LastUpdated == fileInfo.LastWriteTime)
			{
				return pngFile;
			}
			else
			{
				pngFile = await inner.GetPngFile(filename);
				pngFile.Exists = true;
				await pngFileDataSource.WritePngFile(pngFile);
				return pngFile;
			}
		}
	}
}
