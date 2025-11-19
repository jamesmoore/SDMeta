using System;

namespace SDMeta
{
	public class PngFile(string fileName,
        DateTime lastUpdated,
        long length,
        PromptFormat promptFormat,
        string prompt,
        bool exists)
    {
        public string FileName { get; } = fileName;
        public DateTime LastUpdated { get; } = lastUpdated;
        public long Length { get; } = length;
        public string Prompt { get; } = prompt;
        public PromptFormat PromptFormat { get; } = promptFormat;
        /// <summary>
        /// Whether the file exists on the most recent scan
        /// </summary>
        public bool Exists { get; set; } = exists;
    }

	public enum PromptFormat
	{
		None,
		Auto1111,
		ComfyUI,
	}
}
