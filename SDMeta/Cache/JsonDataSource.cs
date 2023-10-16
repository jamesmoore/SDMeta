using NLog;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text.Json;

namespace SDMeta.Cache
{
	public class JsonDataSource : IPngFileDataSource
	{
		private static readonly Logger logger = LogManager.GetCurrentClassLogger();
		private readonly IFileSystem fileSystem;
		private readonly CachePath cachePath;
		private readonly Dictionary<string, PngFile> cache;

		public JsonDataSource(IFileSystem fileSystem)
		{
			this.fileSystem = fileSystem;
			cachePath = new CachePath(fileSystem);
			cache = InitialQuery().ToDictionary(p => p.FileName, p => p);
		}

		public IEnumerable<PngFileSummary> Query(QueryParams queryParams)
		{
			var f = queryParams.Filter;
			return cache.Values.Where(p =>
				string.IsNullOrWhiteSpace(f) ||
				p.FileName.Contains(f) ||
				p.Prompt != null && p.Prompt.Contains(f)
				).
			Select(p => new PngFileSummary()
			{
				FileName = p.FileName,
				FullPromptHash = p.Parameters?.PromptHash + p.Parameters?.NegativePromptHash,
				LastUpdated = p.LastUpdated,
			}
			).ToList();
		}

		private IEnumerable<PngFile> InitialQuery()
		{
			var path = cachePath.GetPath();
			if (fileSystem.File.Exists(path))
			{
				logger.Debug($"Reading cache at {path}");
				var cacheJson = fileSystem.File.ReadAllText(path);
				var deserialised = JsonSerializer.Deserialize<List<PngFileDTO>>(cacheJson);
				var dictionary = deserialised.Select(p => p.ToPngFile()).ToList();
				return dictionary;
			}
			else
			{
				return Enumerable.Empty<PngFile>();
			}
		}

		private void WriteCache(IEnumerable<PngFile> cache)
		{
			var path = cachePath.GetPath();

			logger.Debug($"Flushing cache to {path}");
			var serialized = JsonSerializer.Serialize(cache.Select(p => new PngFileDTO(p)), new JsonSerializerOptions()
			{
				WriteIndented = true,
				DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
			});

			var dirPath = fileSystem.FileInfo.New(path).Directory.FullName;
			if (fileSystem.Directory.Exists(dirPath) == false)
			{
				fileSystem.Directory.CreateDirectory(dirPath);
			}
			fileSystem.File.WriteAllText(path, serialized);
		}

		public PngFile ReadPngFile(string realFileName)
		{
			cache.TryGetValue(realFileName, out PngFile value);
			return value;
		}

		public void WritePngFile(PngFile info)
		{
			if (info != null)
			{
				cache[info.FileName] = info;
			}
		}

		public void Dispose()
		{
			Flush();
		}

		public void Flush()
		{
			WriteCache(cache.Values);
		}

		public void BeginTransaction()
		{
		}

		public void CommitTransaction()
		{
		}

		public IEnumerable<ModelSummary> GetModelSummaryList()
		{
			throw new NotImplementedException();
		}

		public IEnumerable<string> GetAllFilenames()
		{
			return cache.Where(p => p.Value.Exists).Select(p => p.Key).ToList();
		}

		public void Truncate()
		{
			cache.Clear();
		}

		public void PostUpdateProcessing()
		{
		}

		internal class PngFileDTO
		{
			public PngFileDTO(PngFile track)
			{
				FileName = track.FileName;
				LastUpdated = track.LastUpdated;
				Prompt = track.Prompt;
				PromptFormat = track.PromptFormat;
				Length = track.Length;
				Exists = track.Exists;
			}

			public string FileName { get; set; }
			public DateTime LastUpdated { get; set; }
			public long Length { get; set; }
			public string Prompt { get; set; }
			public PromptFormat PromptFormat { get; set; }
			public bool Exists { get; set; }

			public PngFile ToPngFile() => new PngFile()
			{
				FileName = FileName,
				LastUpdated = LastUpdated,
				Prompt = Prompt,
				PromptFormat = PromptFormat,
				Length = Length,
				Exists = Exists,
			};
		}
	}
}