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

		public PngFileViewModel BuildModel(PngFile p)
		{
			var encodedFilename = Base32Encode(p.Filename);
			return new PngFileViewModel()
			{
				Filename = p.Filename,
				ThumbnailUrl = $"/images/thumb/{encodedFilename}",
				LastUpdated = p.LastUpdated,
				Length = p.Length,
				Prompt = p.Parameters?.Prompt ?? "",
				FullPromptHash = p.Parameters?.Prompt + p.Parameters?.NegativePrompt ?? "",
				Tooltip = GetTooltip(p),
				Parameters = p.Parameters,
				ImageUrl = $"/images/full/{encodedFilename}/{fileSystem.Path.GetFileName(p.Filename)}",
			};
		}

		private static string GetTooltip(PngFile p)
		{
			if (p == null) return "";
			return $@"
					<small>
					<strong>Model:</strong> {p.Parameters?.Model ?? ""}<br/>
					<strong>Hash:</strong> {p.Parameters?.ModelHash ?? ""}<br/>
					<strong>Sampler:</strong> {p.Parameters?.Sampler ?? ""}<br/>
					<strong>Date:</strong> {p.LastUpdated}
					</small>";
		}

		private static string Base32Encode(string plainText)
		{
			var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
			return Base32Encoding.ToString(plainTextBytes);
		}

	}
}
