namespace BlazorApp1.Models
{
	public class PngFileViewModel
	{
		public string Filename { get; set; }
		public DateTime LastUpdated { get; set; }
		public long Length { get; set; }
		public string ImageUrl { get; set; }
		public string Prompt { get; set; }
		public string FullPromptHash { get; set; }
		public bool Expanded { get; set; }
	}
}
