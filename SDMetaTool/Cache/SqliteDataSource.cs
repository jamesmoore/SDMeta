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
		private string FTSTableName = $"FTS5{TableName}";
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
			"ModelHash",
			"Model",
			"PromptHash",
			"NegativePromptHash",
			"Version"
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
				DataType: p is "Length" or "[Exists]" or "Version" ? "INTEGER" : "TEXT",
				IsPrimaryKey: p == "FileName"));

			insertSql = $@"INSERT INTO {TableName}({columns.ToCommaSeparated()}) VALUES ( {tabledef.Select(p => p.Parameter).ToCommaSeparated()} )
			ON CONFLICT(FileName) DO UPDATE SET {tabledef.Where(p => p.Column != "FileName").Select(p => p.Column + "=" + p.Parameter).ToCommaSeparated()}, Version=Version+1;
			";

			// Setup table if absent https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/types
			ExecuteOnConnection(connection => connection.Execute(@$"CREATE TABLE IF NOT EXISTS {TableName} (
				{tabledef.Select(p => $"{p.Column} {p.DataType}{(p.IsPrimaryKey ? " PRIMARY KEY" : "")}").ToCommaSeparated()}
				);"));

			ExecuteOnConnection(connection => connection.Execute(@$"CREATE VIRTUAL TABLE IF NOT EXISTS {FTSTableName} USING fts5(FileName,Prompt, NegativePrompt, Params, Version);"));

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
					ModelHash = info.Parameters?.ModelHash,
					Model = info.Parameters?.Model,
					PromptHash = info.Parameters?.PromptHash,
					NegativePromptHash = info.Parameters?.NegativePromptHash,
				};
			}

			public string FileName { get; set; }
			public DateTime LastUpdated { get; set; }
			public long Length { get; set; }
			public bool Exists { get; set; }

			public string Prompt { get; set; }
			public string NegativePrompt { get; set; }
			public string Params { get; set; }

			public string ModelHash { get; set; }
			public string Model { get; set; }

			public string PromptHash { get; set; }
			public string NegativePromptHash { get; set; }
			public int Version { get; set; }

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
					ModelHash = this.ModelHash,
					Model = this.Model,
					PromptHash = this.PromptHash,
					NegativePromptHash = this.NegativePromptHash,
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
			var Sql = BuildQueryStringFTS(queryParams);

			var reader = ExecuteOnConnection(connection => connection.Query<PngFileSummary>(Sql, new
			{
				filter = queryParams.Filter,
				model = queryParams.ModelFilter?.Model,
				modelHash = queryParams.ModelFilter?.ModelHash,
			}));
			return reader;
		}

		private string BuildQueryStringFTS(QueryParams queryParams)
		{
			string sql;
			if (string.IsNullOrWhiteSpace(queryParams.Filter) == false)
			{
				sql = $@"SELECT 
					{TableName}.FileName,
					LastUpdated,
					IFNULL(PromptHash,"") + IFNULL(NegativePromptHash,"") as FullPromptHash
				FROM {TableName}
				join {FTSTableName} on {TableName}.FileName = {FTSTableName}.FileName
				WHERE [Exists] = 1 and {FTSTableName} MATCH @filter";
			}
			else
			{
				sql = $@"SELECT 
					FileName,
					LastUpdated,
					IFNULL(PromptHash,"") + IFNULL(NegativePromptHash,"") as FullPromptHash
				FROM {TableName}
				WHERE [Exists] = 1";
			}

			if (queryParams.ModelFilter != null)
			{
				sql += BuildModelWhereClause(queryParams.ModelFilter);
			}

			return sql;
		}

		private static string BuildModelWhereClause(ModelFilter modelFilter)
		{
			var Sql = string.Empty;
			if (modelFilter.Model == null)
			{
				Sql += " AND Model IS NULL";
			}
			else
			{
				Sql += " AND Model = @model";
			}

			if (modelFilter.ModelHash == null)
			{
				Sql += " AND ModelHash IS NULL";
			}
			else
			{
				Sql += " AND ModelHash = @modelHash";
			}
			return Sql;
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
			ExecuteOnConnection(connection => connection.Execute($"DELETE FROM {FTSTableName}"));
		}

		public void PostUpdateProcessing()
		{
			ExecuteOnConnection(connection =>
				connection.Execute(
					$@"INSERT INTO {FTSTableName} (FileName, Prompt, NegativePrompt, Params, Version) 
					SELECT FileName, Prompt, NegativePrompt, Params, Version FROM {TableName}  
					WHERE FileName NOT IN (SELECT FileName from {FTSTableName})",
				this.transaction));

			ExecuteOnConnection(connection =>
				connection.Execute(
					$@"UPDATE {FTSTableName} SET
						Prompt = p.Prompt,
						NegativePrompt = p.NegativePrompt,
						Params = p.Params,
						Version = p.Version
					FROM {TableName} p
					WHERE {FTSTableName}.FileName = p.FileName and {FTSTableName}.Version != p.Version",
				this.transaction));
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
