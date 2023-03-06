using SDMetaTool;

namespace SDMetaUI.Models
{
	public class PngFileViewModel
	{
		public string FileName { get; set; }
		public DateTime LastUpdated { get; set; }
		public string ThumbnailUrl { get; set; }
		public string ImageUrl { get; set; }
		public string FullPromptHash { get; set; }
		public IList<PngFileViewModel> SubItems { get; set; }
	}
}
