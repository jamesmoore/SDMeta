using Microsoft.AspNetCore.Mvc;
using SDMetaUI.Services;
using System.IO.Abstractions;

namespace SDMetaUI.Controllers
{
	[ApiController]
	[Route("images")]
	public class ImagesController(
		IFileSystem fileSystem,
		IThumbnailService thumbnailService,
		ILogger<ImagesController> _logger
		) : Controller
	{
		[Route("thumb/{path}")]
		public IActionResult Thumb(string path)
		{
			try
			{
				string physicalPath = Base32Decode(path);
				if (fileSystem.File.Exists(physicalPath))
				{
					var fileInfo = fileSystem.FileInfo.New(physicalPath);
					var thumbPath = thumbnailService.GetOrGenerateThumbnail(fileInfo.FullName);
					Response.Headers.LastModified = fileInfo.LastWriteTime.ToUniversalTime().ToString("R");

					return base.PhysicalFile(thumbPath, "image/jpg");
				}
				else
				{
					return NotFound();
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "ImagesController error: ");
				return NotFound();
			}
		}

		[Route("full/{path}/{realfilename}")]
		public IActionResult Full(string path, string realfilename)
		{
			try
			{
				string physicalPath = Base32Decode(path);
				if (fileSystem.File.Exists(physicalPath))
				{
					Response.Headers.LastModified = fileSystem.FileInfo.New(physicalPath).LastWriteTime.ToUniversalTime().ToString("R");
					return base.PhysicalFile(physicalPath, "image/png");
				}
				else
				{
					return NotFound();
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "ImagesController error: ");
				return NotFound();
			}
		}

		public static string Base32Decode(string base64EncodedData)
		{
			var base32EncodedBytes = Base32Encoding.ToBytes(base64EncodedData);
			return System.Text.Encoding.UTF8.GetString(base32EncodedBytes);
		}
	}
}
