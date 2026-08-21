using Microsoft.AspNetCore.StaticFiles;
using SDMetaUI.Services;
using System.IO.Abstractions;

namespace SDMetaUI.Controllers
{
    public class ImagesController()
    {
        private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

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
                    EnableCaching(httpResponse);
                    var fileInfo = fileSystem.FileInfo.New(physicalPath);
                    var thumbPath = thumbnailService.GetOrGenerateThumbnail(fileInfo.FullName);
                    httpResponse.Headers.LastModified = fileInfo.LastWriteTimeUtc.ToString("R");

                    return Results.File(thumbPath, GetContentType(thumbPath));
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
                    EnableCaching(httpResponse);
                    httpResponse.Headers.LastModified = fileSystem.FileInfo.New(physicalPath).LastWriteTimeUtc.ToString("R");
                    return Results.File(physicalPath, GetContentType(physicalPath));
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

        private static string GetContentType(string path)
        {
            return ContentTypeProvider.TryGetContentType(path, out var contentType)
                ? contentType
                : "application/octet-stream";
        }

        private static void EnableCaching(HttpResponse httpResponse)
        {
            httpResponse.Headers.CacheControl = "public, max-age=31536000, immutable";
        }
    }
}
