using SDMeta.Cache;
using System.IO.Abstractions;

namespace SDMeta
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

		public PngFile GetPngFile(string filename)
		{
			var fileInfo = fileSystem.FileInfo.New(filename);
			var pngFile = pngFileDataSource.ReadPngFile(filename);
			if (pngFile != null && pngFile.LastUpdated == fileInfo.LastWriteTime)
			{
				return pngFile;
			}
			else
			{
				pngFile = inner.GetPngFile(filename);
				pngFile.Exists = true;
				pngFileDataSource.WritePngFile(pngFile);
				return pngFile;
			}
		}
	}
}
