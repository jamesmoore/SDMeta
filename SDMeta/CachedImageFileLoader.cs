using SDMeta.Cache;
using System.IO.Abstractions;
using System.Threading.Tasks;

namespace SDMeta
{
    public class CachedImageFileLoader(
        IFileSystem fileSystem,
        IImageFileLoader inner,
        IImageFileDataSource pngFileDataSource) : IImageFileLoader
    {
        public async Task<ImageFile> GetPngFile(string filename)
        {
            var fileInfo = fileSystem.FileInfo.New(filename);
            var pngFile = pngFileDataSource.ReadPngFile(filename);
            if (pngFile != null && pngFile.LastUpdated == fileInfo.LastWriteTime && pngFile.Exists)
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
