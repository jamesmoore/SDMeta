using SDMeta.Cache;
using System.IO.Abstractions;
using System.Threading.Tasks;

namespace SDMeta
{
	public class CachedPngFileLoader(
		IFileSystem fileSystem,
		IPngFileLoader inner,
		IPngFileDataSource pngFileDataSource) : IPngFileLoader
	{
		public async Task<PngFile> GetPngFile(string filename)
		{
			var fileInfo = fileSystem.FileInfo.New(filename);
			var pngFile = pngFileDataSource.ReadPngFile(filename);
			if (pngFile != null && pngFile.LastUpdated == fileInfo.LastWriteTime)
			{
				return pngFile;
			}
			else
			{
				pngFile = await inner.GetPngFile(filename);
				pngFile.Exists = true;
				pngFileDataSource.WritePngFile(pngFile);
				return pngFile;
			}
		}
	}
}
