using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SDMeta.Cache
{
    public partial class SqliteDataSource : IPngFileDataSource
    {
        const string TableName = "PngFilesv2";
        private string FTSTableName = $"FTS5{TableName}";
        private SqliteTransaction? transaction;

        private readonly string[] columns =
        [
            "FileName",
            "LastUpdated",
            "Length",
            "[Exists]",
            "Prompt",
            "PromptFormat",
            "ModelHash",
            "Model",
            "PromptHash",
            "NegativePromptHash",
            "Version",
        ];

        private readonly string[] ftscolumns =
        [
            "FileName",
            "PromptFormat",
            "Prompt",
            "Version"
        ];

        private readonly Lazy<string> insertSql;
        private readonly Lazy<string> ConnectionString;
        private readonly DbPath dbPath;
        private readonly ILogger<SqliteDataSource> logger;
        private readonly IParameterDecoder parameterDecoder;

        public SqliteDataSource(
            DbPath dbPath,
            ILogger<SqliteDataSource> logger,
            IParameterDecoder parameterDecoder)
        {
            this.dbPath = dbPath;
            this.logger = logger;
            this.parameterDecoder = parameterDecoder;
            this.ConnectionString = new Lazy<string>(GetConnectionString);
            this.insertSql = new Lazy<string>(GetInsertSql);
        }

        private string GetConnectionString()
        {
            var path = dbPath.GetPath();
            var connectionString = $"Data Source={path}";
            return connectionString;
        }


        private SqliteConnection GetConnection()
        {
            logger.LogDebug($"Opening connection");
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

        private string GetInsertSql()
        {
            var tabledef = GetTableDefinition();
            return $@"INSERT INTO {TableName}({columns.ToCommaSeparated()}) VALUES ( {tabledef.Select(p => p.Parameter).ToCommaSeparated()} )
			ON CONFLICT(FileName) DO UPDATE SET {tabledef.Where(p => p.Column != "FileName").Select(p => p.Column + "=" + p.Parameter).ToCommaSeparated()}, Version=Version+1;
			";
        }

        public void Initialize()
        {
            logger.LogInformation("Initalizing data source");
            logger.LogInformation("Using db at {path}", this.ConnectionString.Value);

            dbPath.CreateIfMissing();

            var tabledef = GetTableDefinition();

            // Setup table if absent https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/types
            ExecuteOnConnection(connection => connection.Execute(@$"CREATE TABLE IF NOT EXISTS {TableName} (
				{tabledef.Select(p => $"{p.Column} {p.DataType}{(p.IsPrimaryKey ? " PRIMARY KEY" : "")}").ToCommaSeparated()}
				);"));

            ExecuteOnConnection(connection => connection.Execute(@$"CREATE VIRTUAL TABLE IF NOT EXISTS {FTSTableName} USING fts5({ftscolumns.ToCommaSeparated()});"));
            logger.LogInformation("Initalization completed");
        }

        private IEnumerable<ColumnDefinition> GetTableDefinition()
        {
            return columns.Select(p => new ColumnDefinition(
                p,
                "@" + p.Replace("[", "").Replace("]", ""),
                p is "Length" or "[Exists]" or "Version" ? "INTEGER" : "TEXT",
                p == "FileName"));
        }

        public void Dispose()
        {
            logger.LogDebug("Data source dispose");
            this.CommitTransaction();
        }

        public IEnumerable<PngFileSummary> Query(QueryParams queryParams)
        {
            var sql = BuildQueryStringFTS(queryParams);
            var param = new
            {
                filter = queryParams.Filter,
                model = queryParams.ModelFilter?.Model,
                modelHash = queryParams.ModelFilter?.ModelHash,
            };

            var reader = ExecuteOnConnection(connection =>
                connection.Query<PngFileSummary>(sql, param)
            );
            return reader;
        }

        private string BuildQueryStringFTS(QueryParams queryParams)
        {
            string sql;
            if (string.IsNullOrWhiteSpace(queryParams.Filter) == false)
            {
                sql = $@"SELECT 
					{TableName}.FileName,
					IFNULL(PromptHash,"") + IFNULL(NegativePromptHash,"") as FullPromptHash
				FROM {TableName}
				join {FTSTableName} on {TableName}.FileName = {FTSTableName}.FileName
				WHERE [Exists] = 1 and {FTSTableName} MATCH @filter";
            }
            else
            {
                sql = $@"SELECT 
					FileName,
					IFNULL(PromptHash,"") + IFNULL(NegativePromptHash,"") as FullPromptHash
				FROM {TableName}
				WHERE [Exists] = 1";
            }

            if (queryParams.ModelFilter != null)
            {
                sql += BuildModelWhereClause(queryParams.ModelFilter);
            }

            sql += BuildOrderByClause(queryParams.QuerySortBy);

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

        private static string BuildOrderByClause(QuerySortBy querySort)
        {
            return querySort switch
            {
                QuerySortBy.AtoZ => " ORDER BY FileName ASC",
                QuerySortBy.ZtoA => " ORDER BY FileName DESC",
                QuerySortBy.Largest => " ORDER BY Length DESC",
                QuerySortBy.Smallest => " ORDER BY Length ASC",
                QuerySortBy.Newest => " ORDER BY LastUpdated DESC",
                QuerySortBy.Oldest => " ORDER BY LastUpdated ASC",
                QuerySortBy.Random => " ORDER BY Random()",
                _ => String.Empty,
            };
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
                insertSql.Value,
                FromModel(info),
                this.transaction
            ));
        }

        private DataRow FromModel(PngFile info)
        {
            var parameters = parameterDecoder.GetParameters(info);
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
                    $@"INSERT INTO {FTSTableName} (FileName, Prompt, PromptFormat, Version) 
					SELECT FileName, Prompt, PromptFormat, Version FROM {TableName}  
					WHERE FileName NOT IN (SELECT FileName from {FTSTableName})",
                this.transaction));

            ExecuteOnConnection(connection =>
                connection.Execute(
                    $@"UPDATE {FTSTableName} SET
						Prompt = p.Prompt,
						PromptFormat = p.PromptFormat,
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

    internal record struct ColumnDefinition(string Column, string Parameter, string DataType, bool IsPrimaryKey);
}
