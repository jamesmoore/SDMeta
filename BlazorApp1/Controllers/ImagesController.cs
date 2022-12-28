﻿using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using PhotoSauce.MagicScaler;
using System.Drawing;
using System.IO.Abstractions;
using System.Net;

namespace BlazorApp1.Controllers
{
	[ApiController]
	[Route("images")]
	public class ImagesController : Controller
	{
		private readonly IFileSystem fileSystem;

		public ImagesController(IFileSystem fileSystem)
		{
			this.fileSystem = fileSystem;
		}

		[HttpGet]
		[Route("")]
		public IActionResult hello()
		{
			return Content("HELLO");
		}

		[Route("{path}")]
		public IActionResult Index(string path)
		{
			try
			{
				string physicalPath = Base64Decode(path);
				if (fileSystem.File.Exists(physicalPath))
				{
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


		[Route("thumb/{path}")]
		public IActionResult Thumb(string path)
		{
			try
			{
				string physicalPath = Base64Decode(path);
				if (fileSystem.File.Exists(physicalPath))
				{
					var fileInfo = fileSystem.FileInfo.New(physicalPath);
					var name = fileInfo.Name;

					var thumbDir = Path.Combine(
						Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
						"SDMetaTool",
						"cache",
						"thumbs");
					fileSystem.Directory.CreateDirectory(thumbDir);

					var thumbPath = Path.Combine(
						thumbDir,
						name);

					if (fileSystem.File.Exists(thumbPath) == false)
					{
						MagicImageProcessor.ProcessImage(physicalPath, thumbPath, new ProcessImageSettings { Height = 100 });
					}

					return base.PhysicalFile(thumbPath, "image/png");
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
