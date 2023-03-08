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
		private readonly IFileSystem fileSystem;

		private string GetConnectionString()
		{
			var path = new DbPath(fileSystem).GetPath();
			logger.Info($"Using db at {path}");
			var connectionString = $"Data Source={path}";
			return connectionString;
		}

		private readonly Lazy<string> ConnectionString;

		private SqliteConnection GetConnection()
		{
			logger.Info($"Opening connection");
			var connectionString = ConnectionString.Value;
			var connection = new SqliteConnection(connectionString);
			connection.Open();
			return connection;
		}

		private T ExecuteOnConnection<T>(Func<SqliteConnection, T> func)
		{
			if (this.transaction != null)
			{
				return func(transaction.Connection);
			}
			else
			{
				using var connection = GetConnection();
				return func(connection);
			}
		}
		public SqliteDataSource(IFileSystem fileSystem)
		{
			this.fileSystem = fileSystem;
			this.ConnectionString = new Lazy<string>(() => GetConnectionString());

			tabledef = columns.Select(p => (
				Column: p,
				Parameter: "@" + p.Replace("[", "").Replace("]", ""),
				DataType: p == "Length" || p == "[Exists]" ? "INTEGER" : "TEXT",
				IsPrimaryKey: p == "FileName"));

			insertSql = $@"INSERT INTO {TableName}({columns.ToCommaSeparated()}) VALUES ( {tabledef.Select(p => p.Parameter).ToCommaSeparated()} )
			ON CONFLICT(FileName) DO UPDATE SET {tabledef.Where(p => p.Column != "FileName").Select(p => p.Column + "=" + p.Parameter).ToCommaSeparated()};
			";

			// Setup table if absent https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/types
			ExecuteOnConnection(connection => connection.Execute(@$"CREATE TABLE IF NOT EXISTS {TableName} (
				{tabledef.Select(p => p.Column + " " + p.DataType + (p.IsPrimaryKey ? " PRIMARY KEY" : "")).ToCommaSeparated()}
				);"));
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
				var hasParams = string.IsNullOrWhiteSpace(this.Prompt) == false ||
					string.IsNullOrWhiteSpace(this.NegativePrompt) == false ||
					string.IsNullOrWhiteSpace(this.Params) == false;
				if (hasParams == false) return null;
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
			logger.Info("Data source dispose");
			this.CommitTransaction();
		}

		public IEnumerable<PngFileSummary> Query(QueryParams queryParams)
		{
			var Sql = $@"SELECT 
					FileName,
					LastUpdated,
					IFNULL(PromptHash,"") + IFNULL(NegativePromptHash,"") as FullPromptHash
				FROM {TableName}
				WHERE [Exists] = 1";

			if (string.IsNullOrWhiteSpace(queryParams.Filter) == false)
			{
				Sql += " AND ( FileName LIKE '%' || @filter || '%' OR Prompt LIKE '%' || @filter || '%' OR Seed = @filter)";
			}

			if (queryParams.ModelFilter != null)
			{
				if (queryParams.ModelFilter.Model == null)
				{
					Sql += " AND Model IS NULL";
				}
				else
				{
					Sql += " AND Model = @model";
				}

				if (queryParams.ModelFilter.ModelHash == null)
				{
					Sql += " AND ModelHash IS NULL";
				}
				else
				{
					Sql += " AND ModelHash = @modelHash";
				}
			}

			var reader = ExecuteOnConnection(connection => connection.Query<PngFileSummary>(Sql, new
			{
				filter = queryParams.Filter,
				model = queryParams.ModelFilter?.Model,
				modelHash = queryParams.ModelFilter?.ModelHash,
			}));
			return reader;
		}

		public PngFile ReadPngFile(string realFileName)
		{
			var reader = ExecuteOnConnection(connection => connection.QueryFirstOrDefault<DataRow>(
			$@"SELECT *
				FROM {TableName}
				WHERE FileName = @FileName
			", new { FileName = realFileName }));

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
			ExecuteOnConnection(connection => connection.Execute(
				insertSql,
				DataRow.FromModel(info),
				this.transaction
			));
		}

		public void BeginTransaction()
		{
			this.transaction ??= GetConnection().BeginTransaction();
		}

		public void CommitTransaction()
		{
			if (this.transaction != null)
			{
				var connection = this.transaction.Connection;
				this.transaction.Commit();
				this.transaction.Dispose();
				this.transaction = null;
				connection.Close();
				connection.Dispose();
			}
		}

		public IEnumerable<ModelSummary> GetModelSummaryList()
		{
			var reader = ExecuteOnConnection(connection => connection.Query<ModelSummary>(
				$@"SELECT Model, ModelHash, Count(*) as Count
				FROM {TableName}
				GROUP BY Model, ModelHash
				ORDER BY 3 DESC"
				));

			return reader;
		}

		public IEnumerable<string> GetAllFilenames()
		{
			var reader = ExecuteOnConnection(connection => connection.Query<string>(
				$@"SELECT Filename
				FROM {TableName}
				WHERE [Exists] = 1"
				));

			return reader;
		}

		public void Truncate()
		{
			ExecuteOnConnection(connection => connection.Execute($"DELETE FROM {TableName}"));
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
