using System;

namespace SDMetaTool
{
	public class PngFileSummary
	{
		public string FileName { get; set; }
		public DateTime LastUpdated { get; set; }
		public long Length { get; set; }
		public string FullPromptHash { get; set; }
		public string Model { get; set; }
		public string ModelHash { get; set; }
	}
}
