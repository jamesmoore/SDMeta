using NLog;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text.Json;

namespace SDMetaTool.Cache
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
			this.cachePath = new CachePath(fileSystem);
			cache = this.InitialQuery().ToDictionary(p => p.FileName, p => p);
		}

		public IEnumerable<PngFileSummary> Query(QueryParams queryParams)
		{
			var f = queryParams.Filter;
			return cache.Values.Where(p =>
				string.IsNullOrWhiteSpace(f) ||
				p.FileName.Contains(f) ||
				p.Parameters != null && (
					(p.Parameters.Seed == f) ||
					(p.Parameters.Prompt.Contains(f))
				)).
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
				var dictionary = deserialised.Select(PngFileDTOToPngFile).ToList();
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
			var serialized = JsonSerializer.Serialize(cache.Select(PngFileToPngFileDTO), new JsonSerializerOptions()
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
			this.WriteCache(cache.Values);
		}

		private static PngFile PngFileDTOToPngFile(PngFileDTO trackDTO)
		{
			return new PngFile()
			{
				FileName = trackDTO.FileName,
				LastUpdated = trackDTO.LastUpdated,
				Parameters = trackDTO.Parameters,
				Length = trackDTO.Length,
				Exists = trackDTO.Exists,
			};
		}

		private static PngFileDTO PngFileToPngFileDTO(PngFile track)
		{
			return new PngFileDTO()
			{
				FileName = track.FileName,
				LastUpdated = track.LastUpdated,
				Parameters = track.Parameters,
				Length = track.Length,
				Exists = track.Exists,
			};
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
			this.cache.Clear();
		}

		public void PostUpdateProcessing()
		{
		}

		internal class PngFileDTO
		{
			public string FileName { get; set; }
			public DateTime LastUpdated { get; set; }
			public long Length { get; set; }
			public GenerationParams Parameters { get; set; }
			public bool Exists { get; set; }
		}
	}
}