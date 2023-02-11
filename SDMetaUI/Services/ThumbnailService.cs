using PhotoSauce.MagicScaler;
using SDMetaTool.Cache;
using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Text;

namespace SDMetaUI.Services
{
	public class ThumbnailService : IThumbnailService
	{
		private readonly IFileSystem fileSystem;

		public ThumbnailService(IFileSystem fileSystem)
		{
			this.fileSystem = fileSystem;
		}

		public string GetOrGenerateThumbnail(string fullName)
		{
			var thumbnailFullName = GetThumbnailFileName(fullName);

			if (fileSystem.File.Exists(thumbnailFullName) == false)
			{
				MagicImageProcessor.ProcessImage(fullName, thumbnailFullName, new ProcessImageSettings { Height = 175, Width = 175 });
			}

			return thumbnailFullName;
		}

		public void Delete(string fullName)
		{
			var thumbnailFullName = GetThumbnailFileName(fullName);

			if (fileSystem.File.Exists(thumbnailFullName))
			{
				this.fileSystem.File.Delete(thumbnailFullName);
			}
		}

		private string GetThumbnailFileName(string fullName)
		{
			var thumbnailName = HashWithSHA256(fullName) + ".jpg";

			var thumbDir = Path.Combine(
				new DataPath(fileSystem).GetPath(),
				"cache",
				"thumbs");
			fileSystem.Directory.CreateDirectory(thumbDir);

			var thumbnailFullName = Path.Combine(
				thumbDir,
				thumbnailName);
			return thumbnailFullName;
		}

		public static string HashWithSHA256(string value)
		{
			using var hash = SHA256.Create();
			var byteArray = hash.ComputeHash(Encoding.UTF8.GetBytes(value));
			return Base32Encoding.ToString(byteArray);
		}
	}
}
