using SDMetaTool;

namespace SDMetaUI.Models
{
	public class PngFileViewModel
	{
		public string Filename { get; set; }
		public DateTime LastUpdated { get; set; }
		public long Length { get; set; }
		public string ThumbnailUrl { get; set; }
		public string ImageUrl { get; set; }
		public string Prompt { get; set; }
		public string FullPromptHash { get; set; }
		public bool Expanded { get; set; }
		public string Tooltip { get; set; }
		public GenerationParams Parameters { get; set; }


	}
}
