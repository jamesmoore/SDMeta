using SDMeta;
using System;

namespace SDMetaTool.Cache
{
	public partial class SqliteDataSource
	{
		private class DataRow
		{
			public static DataRow FromModel(PngFile info)
			{
				var parameters = info.Parameters;
				return new DataRow()
				{
					FileName = info.FileName,
					LastUpdated = info.LastUpdated,
					Length = info.Length,
					Exists = info.Exists,
					Prompt = info.Prompt,
					PromptFormat = info.PromptFormat.ToString(),
					ModelHash = parameters?.ModelHash,
					Model = parameters?.Model,
					PromptHash = parameters?.PromptHash,
					NegativePromptHash = parameters?.NegativePromptHash,
				};
			}

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

			internal PngFile ToModel() => new PngFile(
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
