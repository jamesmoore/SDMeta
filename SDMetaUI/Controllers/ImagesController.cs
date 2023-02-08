using Microsoft.AspNetCore.Mvc;
using SDMetaUI.Services;
using System.IO.Abstractions;

namespace SDMetaUI.Controllers
{
	[ApiController]
	[Route("images")]
	public class ImagesController : Controller
	{
		private readonly IFileSystem fileSystem;
		private readonly IThumbnailService thumbnailService;

		public ImagesController(IFileSystem fileSystem, IThumbnailService thumbnailService)
		{
			this.fileSystem = fileSystem;
			this.thumbnailService = thumbnailService;
		}

		[Route("thumb/{path}")]
		public IActionResult Thumb(string path)
		{
			try
			{
				string physicalPath = Base64Decode(path);
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
				Console.WriteLine(ex.Message);
				return NotFound();
			}
		}

		[Route("full/{path}")]
		public IActionResult Full(string path)
		{
			try
			{
				string physicalPath = Base64Decode(path);
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
				Console.WriteLine(ex.Message);
				return NotFound();
			}
		}

		public static string Base64Decode(string base64EncodedData)
		{
			var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
			return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
		}


	}
}
