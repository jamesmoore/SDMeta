using SDMetaTool;
using System.IO.Abstractions;

namespace SDMetaUI.Models
{
	public class PngFileViewModelBuilder
	{
		private readonly IFileSystem fileSystem;

		public PngFileViewModelBuilder(IFileSystem fileSystem)
		{
			this.fileSystem = fileSystem;
		}

		public PngFileViewModel BuildModel(PngFileSummary p)
		{
			var encodedFileName = Base32Encode(p.FileName);
			return new PngFileViewModel()
			{
				FileName = p.FileName,
				ThumbnailUrl = $"/images/thumb/{encodedFileName}",
				FullPromptHash = p.FullPromptHash,
				ImageUrl = $"/images/full/{encodedFileName}/{fileSystem.Path.GetFileName(p.FileName)}",
			};
		}

		private static string Base32Encode(string plainText)
		{
			var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
			return Base32Encoding.ToString(plainTextBytes);
		}

	}
}
