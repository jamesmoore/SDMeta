using System;

namespace SDMeta
{
	public class PngFile
	{
		public PngFile(string fileName,
			DateTime lastUpdated,
			long length,
			PromptFormat promptFormat,
			string prompt,
			bool exists
			)
		{
			FileName = fileName;
			LastUpdated = lastUpdated;
			Length = length;
			PromptFormat = promptFormat;
			Prompt = prompt;
			Exists = exists;
		}

		public string FileName { get; }
		public DateTime LastUpdated { get; }
		public long Length { get; }
		public string Prompt { get; }
		public PromptFormat PromptFormat { get; }
		/// <summary>
		/// Whether the file exists on the most recent scan
		/// </summary>
		public bool Exists { get; set; }
	}

	public enum PromptFormat
	{
		None,
		Auto1111,
		ComfyUI,
	}
}
