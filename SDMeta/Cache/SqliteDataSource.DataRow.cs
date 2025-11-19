using SDMeta;
using System;

namespace SDMeta.Cache
{
	public partial class SqliteDataSource
	{
		private class DataRow
		{

			public string FileName { get; set; }
			public DateTime LastUpdated { get; set; }
			public long Length { get; set; }
			public bool Exists { get; set; }

			public string Prompt { get; set; }
			public string PromptFormat { get; set; }

			public string ModelHash { get; set; }
			public string Model { get; set; }

			public string PromptHash { get; set; }
			public string NegativePromptHash { get; set; }
			public int Version { get; set; }

			internal PngFile ToModel() => new(
				this.FileName,
				this.LastUpdated,
				this.Length,
				Enum.Parse<PromptFormat>(this.PromptFormat),
				this.Prompt,
				this.Exists
			);
		}
	}
}
