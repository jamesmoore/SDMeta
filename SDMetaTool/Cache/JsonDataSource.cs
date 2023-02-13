using NLog;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;

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
			cache = this.InitialGetAll().ToDictionary(p => p.FileName, p => p);
		}

		public async Task<IEnumerable<PngFile>> GetAll()
		{
			return cache.Values;
		}

		private IEnumerable<PngFile> InitialGetAll()
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

		private async Task WriteCache(IEnumerable<PngFile> cache)
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

		public async Task<PngFile> ReadPngFile(string realFileName)
		{
			cache.TryGetValue(realFileName, out PngFile value);
			return value;
		}

		public async Task WritePngFile(PngFile info)
		{
			if (info != null)
			{
				cache[info.FileName] = info;
			}
		}

		public async ValueTask DisposeAsync()
		{
			await this.WriteCache(cache.Values);
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

		public async Task BeginTransaction()
		{
		}

		public async Task CommitTransaction()
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
