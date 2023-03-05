using System;

namespace SDMetaTool
{
	public class PngFileSummary
	{
		public string FileName { get; set; }
		public DateTime LastUpdated { get; set; }
		public long Length { get; set; }
		public string Prompt { get; set; }
		public string FullPromptHash { get; set; }
		public string Model { get; set; }
		public string ModelHash { get; set; }
		public string Seed { get; set; }
	}
}
