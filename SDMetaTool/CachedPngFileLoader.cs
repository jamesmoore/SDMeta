using SDMetaTool.Cache;
using System.IO.Abstractions;

namespace SDMetaTool
{
	public class CachedPngFileLoader : IPngFileLoader
	{
		private readonly IPngFileLoader inner;
		private readonly IPngFileDataSource pngFileCache;
		private readonly IFileSystem fileSystem;

		public CachedPngFileLoader(
			IFileSystem fileSystem,
			IPngFileLoader inner,
			IPngFileDataSource pngFileCache)
		{
			this.inner = inner;
			this.pngFileCache = pngFileCache;
			this.fileSystem = fileSystem;
		}

		public PngFile GetPngFile(string filename)
		{
			var fileInfo = fileSystem.FileInfo.New(filename);
			var pngFile = pngFileCache.ReadPngFile(filename);
			if (pngFile != null && pngFile.LastUpdated == fileInfo.LastWriteTime)
			{
				return pngFile;
			}
			else
			{
				pngFile = inner.GetPngFile(filename);
				pngFileCache.WritePngFile(pngFile);
				return pngFile;
			}
		}
	}
}
