using SDMetaTool;

namespace SDMetaUI.Models
{
	public class PngFileViewModel
	{
		public string Filename { get; set; }
		public DateTime LastUpdated { get; set; }
		public long Length { get; set; }
		public string ThumbnailUrl { get; set; }
		public string Prompt { get; set; }
		public string FullPromptHash { get; set; }
		public bool Expanded { get; set; }
		public string Tooltip { get; set; }

		public static PngFileViewModel FromModel(PngFile p)
		{
			return new PngFileViewModel()
			{
				Filename = p.Filename,
				ThumbnailUrl = $"/images/thumb/{Base64Encode(p.Filename)}",
				LastUpdated = p.LastUpdated,
				Length = p.Length,
				Prompt = p.Parameters?.Prompt ?? "",
				FullPromptHash = p.Parameters?.Prompt + p.Parameters?.NegativePrompt ?? "",
				Tooltip = $@"
					<small>
					<strong>Model:</strong> {p.Parameters?.Model ?? ""}<br/>
					<strong>Hash:</strong> {p.Parameters?.ModelHash ?? ""}<br/>
					<strong>Sampler:</strong> {p.Parameters?.Sampler ?? ""}<br/>
					<strong>Date:</strong> {p.LastUpdated}
					</small>",
			};
		}

		public string GetImageUrl()
		{
			return $"/images/full/{Base64Encode(this.Filename)}";
		}

		private static string Base64Encode(string plainText)
		{
			var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
			return System.Convert.ToBase64String(plainTextBytes);
		}
	}
}
