using PhotoSauce.MagicScaler;
using SDMeta.Cache;
using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Text;

namespace SDMetaUI.Services
{
	public class ThumbnailService(IFileSystem fileSystem, DataPath dataPath) : IThumbnailService
	{
		public const int ThumbnailSize = 175;

		public string GetOrGenerateThumbnail(string fullName)
		{
			var thumbnailFullName = GetThumbnailFileName(fullName);

			if (fileSystem.File.Exists(thumbnailFullName) == false)
			{
				var result = MagicImageProcessor.ProcessImage(fullName, thumbnailFullName, new ProcessImageSettings { Height = ThumbnailSize, Width = ThumbnailSize });
			}

			return thumbnailFullName;
		}

		public void Delete(string fullName)
		{
			var thumbnailFullName = GetThumbnailFileName(fullName);

			if (fileSystem.File.Exists(thumbnailFullName))
			{
				fileSystem.File.Delete(thumbnailFullName);
			}
		}

		private string GetThumbnailFileName(string fullName)
		{
			var thumbnailName = HashWithSHA256(fullName) + ".jpg";

			var thumbDir = fileSystem.Path.Combine(
				GetThumbnailDirectory(),
				thumbnailName[..2]
				);
			fileSystem.Directory.CreateDirectory(thumbDir);

			var thumbnailFullName = fileSystem.Path.Combine(
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

		public void DeleteThumbs()
		{
			var thumbDir = GetThumbnailDirectory();

			if (fileSystem.Directory.Exists(thumbDir))
			{
				fileSystem.Directory.Delete(thumbDir, true);
			}
		}

		public string GetThumbnailDirectory()
		{
			return fileSystem.Path.Combine(
				dataPath.GetPath(),
				"cache",
				"thumbs"
				);
		}
	}
}
