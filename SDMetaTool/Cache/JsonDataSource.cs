using NLog;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text.Json;

namespace SDMetaTool.Cache
{
	public class JsonDataSource : IPngFileDataSource, IDisposable
	{
		private static readonly Logger logger = LogManager.GetCurrentClassLogger();
		private readonly IFileSystem fileSystem;
		private readonly CachePath cachePath;
		private readonly Dictionary<string, PngFile> cache;

		public JsonDataSource(IFileSystem fileSystem)
		{
			this.fileSystem = fileSystem;
			this.cachePath = new CachePath(fileSystem);
			cache = this.InitialGetAll().ToDictionary(p => p.Filename, p => p);
		}

		public IEnumerable<PngFile> GetAll()
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
				cache[info.Filename] = info;
			}
		}

		public void ClearAll()
		{
			foreach (var item in cache)
			{
				item.Value.Exists = false;
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
				Filename = trackDTO.Filename,
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
				Filename = track.Filename,
				LastUpdated = track.LastUpdated,
				Parameters = track.Parameters,
				Length = track.Length,
				Exists = track.Exists,
			};
		}


		internal class PngFileDTO
		{
			public string Filename { get; set; }
			public DateTime LastUpdated { get; set; }
			public long Length { get; set; }
			public GenerationParams Parameters { get; set; }
			public bool Exists { get; set; }
		}
	}
}
