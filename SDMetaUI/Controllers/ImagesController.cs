using SDMetaUI.Services;
using System.IO.Abstractions;

namespace SDMetaUI.Controllers
{
    public class ImagesController()
    {
        public static IResult GetThumb(
            IFileSystem fileSystem,
            IThumbnailService thumbnailService,
            ILogger<ImagesController> _logger,
            string path,
            HttpResponse httpResponse)
        {
            try
            {
                string physicalPath = Base32Decode(path);
                if (fileSystem.File.Exists(physicalPath))
                {
                    var fileInfo = fileSystem.FileInfo.New(physicalPath);
                    var thumbPath = thumbnailService.GetOrGenerateThumbnail(fileInfo.FullName);
                    httpResponse.Headers.LastModified = fileInfo.LastWriteTimeUtc.ToString("R");

                    return Results.File(thumbPath, "image/jpg");
                }
                else
                {
                    return TypedResults.NotFound();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ImagesController error: ");
                return TypedResults.Problem();
            }
        }

        public static IResult GetFull(
            IFileSystem fileSystem,
            ILogger<ImagesController> _logger,
            string path,
            HttpResponse httpResponse)
        {
            try
            {
                string physicalPath = Base32Decode(path);
                if (fileSystem.File.Exists(physicalPath))
                {
                    httpResponse.Headers.LastModified = fileSystem.FileInfo.New(physicalPath).LastWriteTimeUtc.ToString("R");
                    return Results.File(physicalPath, "image/png");
                }
                else
                {
                    return TypedResults.NotFound();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ImagesController error: ");
                return TypedResults.Problem();
            }
        }

        private static string Base32Decode(string base32EncodedData)
        {
            var base32EncodedBytes = Base32Encoding.ToBytes(base32EncodedData);
            return System.Text.Encoding.UTF8.GetString(base32EncodedBytes);
        }
    }
}
