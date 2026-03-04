using SDMeta.Cache;
using System.IO.Abstractions;
using System.Threading.Tasks;

namespace SDMeta
{
    public class CachedImageFileLoader(
        IFileSystem fileSystem,
        IImageFileLoader inner,
        IImageFileDataSource imageFileDataSource) : IImageFileLoader
    {
        public async Task<ImageFile> GetImageFile(string filename)
        {
            var fileInfo = fileSystem.FileInfo.New(filename);
            var imageFile = imageFileDataSource.ReadImageFile(filename);
            if (imageFile != null && imageFile.LastUpdated == fileInfo.LastWriteTime && imageFile.Exists)
            {
                return imageFile;
            }
            else
            {
                imageFile = await inner.GetImageFile(filename);
                imageFile.Exists = true;
                imageFileDataSource.WriteImageFile(imageFile);
                return imageFile;
            }
        }
    }
}
