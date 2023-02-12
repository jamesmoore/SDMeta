using Dapper;
using Microsoft.Data.Sqlite;
using NLog;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;

namespace SDMetaTool.Cache
{
	public class SqliteDataSource : IPngFileDataSource
	{
		const string TableName = "PngFiles";
		private readonly SqliteConnection connection;
		private SqliteTransaction transaction;
		private static readonly Logger logger = LogManager.GetCurrentClassLogger();

		private readonly string[] columns = new string[]
		{
			"FileName",
			"LastUpdated",
			"Length",
			"[Exists]",
			"Prompt",
			"NegativePrompt",
			"Params",
			"Warnings",
			"Steps",
			"Sampler",
			"CFGScale",
			"Seed",
			"Size",
			"ModelHash",
			"Model",
			"ClipSkip",
			"DenoisingStrength",
			"BatchSize",
			"BatchPos",
			"FaceRestoration",
			"Eta",
			"FirstPassSize",
			"ENSD",
			"Hypernet",
			"HypernetHash",
			"HypernetStrength",
			"MaskBlur",
			"VariationSeed",
			"VariationSeedStrength",
			"SeedResizeFrom",
			"HiresResize",
			"HiresUpscaler",
			"HiresUpscale",
			"HiresSteps",
			"PromptHash",
			"NegativePromptHash",
		};

		private readonly IEnumerable<(string Column, string Parameter, string DataType, bool IsPrimaryKey)> tabledef;
		private readonly string insertSql;

		public SqliteDataSource(IFileSystem fileSystem)
		{
			var path = new DbPath(fileSystem).GetPath();

			logger.Debug($"Using db at {path}");

			connection = new SqliteConnection($"Data Source={path}");
			connection.Open();

			tabledef = columns.Select(p => (
				Column: p,
				Parameter: "@" + p.Replace("[", "").Replace("]", ""),
				DataType: p == "Length" || p == "[Exists]" ? "INTEGER" : "TEXT",
				IsPrimaryKey: p == "FileName" ));

			insertSql = $@"INSERT INTO {TableName}({columns.ToCommaSeparated()}) VALUES ( {tabledef.Select(p => p.Parameter).ToCommaSeparated()} )
			ON CONFLICT(FileName) DO UPDATE SET {tabledef.Where(p => p.Column != "FileName").Select(p => p.Column + "=" + p.Parameter).ToCommaSeparated()};
			";

			// Setup table if absent https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/types
			connection.Execute(@$"CREATE TABLE IF NOT EXISTS {TableName} (
				{tabledef.Select(p => p.Column + " " + p.DataType + (p.IsPrimaryKey ? " PRIMARY KEY" : "")).ToCommaSeparated()}
				);");
		}

		private class DataRow
		{
			public static DataRow FromModel(PngFile info)
			{
				return new DataRow()
				{
					FileName = info.FileName,
					LastUpdated = info.LastUpdated,
					Length = info.Length,
					Exists = info.Exists,
					Prompt = info.Parameters?.Prompt,
					NegativePrompt = info.Parameters?.NegativePrompt,
					Params = info.Parameters?.Params,
					Warnings = info.Parameters?.Warnings,
					Steps = info.Parameters?.Steps,
					Sampler = info.Parameters?.Sampler,
					CFGScale = info.Parameters?.CFGScale,
					Seed = info.Parameters?.Seed,
					Size = info.Parameters?.Size,
					ModelHash = info.Parameters?.ModelHash,
					Model = info.Parameters?.Model,
					ClipSkip = info.Parameters?.ClipSkip,
					DenoisingStrength = info.Parameters?.DenoisingStrength,
					BatchSize = info.Parameters?.BatchSize,
					BatchPos = info.Parameters?.BatchPos,
					FaceRestoration = info.Parameters?.FaceRestoration,
					Eta = info.Parameters?.Eta,
					FirstPassSize = info.Parameters?.FirstPassSize,
					ENSD = info.Parameters?.ENSD,
					Hypernet = info.Parameters?.Hypernet,
					HypernetHash = info.Parameters?.HypernetHash,
					HypernetStrength = info.Parameters?.HypernetStrength,
					MaskBlur = info.Parameters?.MaskBlur,
					VariationSeed = info.Parameters?.VariationSeed,
					VariationSeedStrength = info.Parameters?.VariationSeedStrength,
					SeedResizeFrom = info.Parameters?.SeedResizeFrom,
					HiresResize = info.Parameters?.HiresResize,
					HiresUpscaler = info.Parameters?.HiresUpscaler,
					PromptHash = info.Parameters?.PromptHash,
					NegativePromptHash = info.Parameters?.NegativePromptHash,
					HiresUpscale = info.Parameters?.HiresUpscale,
					HiresSteps = info.Parameters?.HiresSteps
				};
			}

			public string FileName { get; set; }
			public DateTime LastUpdated { get; set; }
			public long Length { get; set; }
			public bool Exists { get; set; }

			public string Prompt { get; set; }
			public string NegativePrompt { get; set; }
			public string Params { get; set; }
			public string Warnings { get; set; }

			public string Steps { get; set; }
			public string Sampler { get; set; }
			public string CFGScale { get; set; }
			public string Seed { get; set; }
			public string Size { get; set; }
			public string ModelHash { get; set; }
			public string Model { get; set; }
			public string ClipSkip { get; set; }
			public string DenoisingStrength { get; set; }
			public string BatchSize { get; set; }
			public string BatchPos { get; set; }
			public string FaceRestoration { get; set; }
			public string Eta { get; set; }
			public string FirstPassSize { get; set; }
			public string ENSD { get; set; }
			public string Hypernet { get; set; }
			public string HypernetHash { get; set; }
			public string HypernetStrength { get; set; }
			public string MaskBlur { get; set; }
			public string VariationSeed { get; set; }
			public string VariationSeedStrength { get; set; }
			public string SeedResizeFrom { get; set; }
			public string HiresResize { get; set; }
			public string HiresUpscaler { get; set; }

			public string PromptHash { get; set; }
			public string NegativePromptHash { get; set; }
			public string HiresUpscale { get; set; }
			public string HiresSteps { get; set; }

			internal PngFile ToModel()
			{
				return new PngFile()
				{
					FileName = this.FileName,
					LastUpdated = this.LastUpdated,
					Length = this.Length,
					Exists = this.Exists,
					Parameters = this.ToGenerationParams()
				};
			}

			internal GenerationParams ToGenerationParams()
			{
				if(string.IsNullOrWhiteSpace(this.Prompt) || string.IsNullOrWhiteSpace(this.NegativePrompt) || string.IsNullOrWhiteSpace(this.Params)) return null;
				return new GenerationParams()
				{
					Prompt = this.Prompt,
					NegativePrompt = this.NegativePrompt,
					Params = this.Params,
					Warnings = this.Warnings,
					Steps = this.Steps,
					Sampler = this.Sampler,
					CFGScale = this.CFGScale,
					Seed = this.Seed,
					Size = this.Size,
					ModelHash = this.ModelHash,
					Model = this.Model,
					ClipSkip = this.ClipSkip,
					DenoisingStrength = this.DenoisingStrength,
					BatchSize = this.BatchSize,
					BatchPos = this.BatchPos,
					FaceRestoration = this.FaceRestoration,
					Eta = this.Eta,
					FirstPassSize = this.FirstPassSize,
					ENSD = this.ENSD,
					Hypernet = this.Hypernet,
					HypernetHash = this.HypernetHash,
					HypernetStrength = this.HypernetStrength,
					MaskBlur = this.MaskBlur,
					VariationSeed = this.VariationSeed,
					VariationSeedStrength = this.VariationSeedStrength,
					SeedResizeFrom = this.SeedResizeFrom,
					HiresResize = this.HiresResize,
					HiresUpscaler = this.HiresUpscaler,
					PromptHash = this.PromptHash,
					NegativePromptHash = this.NegativePromptHash,
					HiresUpscale = this.HiresUpscale,
					HiresSteps = this.HiresSteps,
				};
			}
		}

		public void Dispose()
		{
			this.CommitTransaction();
			connection.Dispose();
		}

		public IEnumerable<PngFile> GetAll()
		{
			var reader = connection.Query<DataRow>(
				$@"SELECT *
				FROM {TableName}"
				);

			return reader.Select(p => p.ToModel());
		}

		public PngFile ReadPngFile(string realFileName)
		{
			var reader = connection.QueryFirstOrDefault<DataRow>(
			$@"SELECT *
				FROM {TableName}
				WHERE FileName = @FileName
			", new { FileName = realFileName });

			if (reader != null)
			{
				return reader.ToModel();
			}
			else
			{
				return null;
			}
		}

		public void WritePngFile(PngFile info)
		{
			connection.Execute(
				insertSql,
				DataRow.FromModel(info),
				this.transaction
			);
		}

		public void BeginTransaction()
		{
			if (this.transaction == null)
			{
				this.transaction = this.connection.BeginTransaction();
			}
		}

		public void CommitTransaction()
		{
			if (this.transaction != null)
			{
				this.transaction.Commit();
				this.transaction.Dispose();
				this.transaction = null;
			}
		}
	}

	public static class ExtensionMethods
	{
		public static string ToCommaSeparated(this IEnumerable<string> list)
		{
			return string.Join(",", list);
		}
	}
}
