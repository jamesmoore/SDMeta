using SDMetaTool.Auto1111;
using System;

namespace SDMeta
{
	public partial class PngFile
	{
		public PngFile()
		{
			generationParams = new Lazy<GenerationParams>(GetParams);
		}

		public string FileName { get; set; }
		public DateTime LastUpdated { get; set; }
		public long Length { get; set; }
		public GenerationParams Parameters => generationParams.Value;

		private Lazy<GenerationParams> generationParams;

		private GenerationParams GetParams()
		{
			if (PromptFormat == PromptFormat.Auto1111)
			{
				return new Auto1111ParameterDecoder().GetParameters(Prompt);
			}
			else
			{
				return null;
			}
		}

		public string Prompt { get; set; }
		public PromptFormat PromptFormat { get; set; }
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
