using SDMeta.Cache;
using System.IO.Abstractions;
using System.Threading.Tasks;

namespace SDMeta
{
    public class CachedImageFileLoader(
        IImageFileLoader inner,
        IImageFileDataSource imageFileDataSource) : IImageFileLoader
    {
        public async Task<ImageFile> GetImageFile(IFileInfo fileInfo)
        {
            var imageFile = imageFileDataSource.ReadImageFile(fileInfo.FullName);
            if (imageFile != null && imageFile.LastUpdated == fileInfo.LastWriteTime && imageFile.Exists)
            {
                return imageFile;
            }
            else
            {
                imageFile = await inner.GetImageFile(fileInfo);
                imageFile.Exists = true;
                imageFileDataSource.WriteImageFile(imageFile);
                return imageFile;
            }
        }
    }
}
