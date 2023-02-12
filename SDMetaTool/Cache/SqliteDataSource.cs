using Dapper;
using Microsoft.Data.Sqlite;
using NLog;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace SDMetaTool.Cache
{
	public class SqliteDataSource : IPngFileDataSource
	{
		const string TableName = "PngFiles";
		private readonly SqliteConnection connection;
		private SqliteTransaction transaction;
		private static readonly Logger logger = LogManager.GetCurrentClassLogger();

		public SqliteDataSource(IFileSystem fileSystem)
		{
			var path = new DbPath(fileSystem).GetPath();

			logger.Debug($"Using db at {path}");

			connection = new SqliteConnection($"Data Source={path}");
			connection.Open();

			// Setup table if absent https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/types
			connection.Execute($"CREATE TABLE IF NOT EXISTS {TableName} (Filename TEXT primary key, FileExists INTEGER, Data BLOB);");
		}

		private class DataRow
		{
			public string Filename { get; set; }
			public bool FileExists { get; set; }
			public byte[] Data { get; set; }
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

			return reader.Select(p => Deserialize(p.Data));
		}

		public PngFile ReadPngFile(string realFileName)
		{
			var reader = connection.QueryFirstOrDefault<DataRow>(
			$@"SELECT *
				FROM {TableName}
				WHERE Filename = @Filename
			", new { Filename = realFileName });

			if (reader != null)
			{
				return Deserialize(reader.Data);
			}
			else
			{
				return null;
			}
		}

		public void WritePngFile(PngFile info)
		{
			var command = connection.Execute(
			$@"INSERT INTO {TableName}(Filename,FileExists,Data) VALUES (@Filename,@FileExists,@Data)
			ON CONFLICT(Filename) DO UPDATE SET Data=@Data,FileExists=@FileExists;
			",
			new DataRow
			{
				Filename = info.Filename,
				FileExists = info.Exists,
				Data = Serialize(info)
			},
			this.transaction
			);
		}

		private static PngFile Deserialize(byte[] data)
		{
			var json = Encoding.UTF8.GetString(data);
			var pngFile = JsonSerializer.Deserialize<PngFile>(json);
			return pngFile;
		}

		private static byte[] Serialize(PngFile pngFile)
		{
			var json = JsonSerializer.Serialize(pngFile);
			var bytes = Encoding.UTF8.GetBytes(json);
			return bytes;
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
}
