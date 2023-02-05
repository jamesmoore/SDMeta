using Microsoft.Data.Sqlite;
using NLog;
using System.Collections.Generic;
using System.IO.Abstractions;
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
			var createCommand = connection.CreateCommand();
			createCommand.CommandText = $"CREATE TABLE IF NOT EXISTS {TableName} (Filename TEXT primary key, FileExists INTEGER, Data BLOB);";
			createCommand.ExecuteNonQuery();
		}

		public void Dispose()
		{
			this.CommitTransaction();
			connection.Dispose();
		}

		public IEnumerable<PngFile> GetAll()
		{
			var command = connection.CreateCommand();
			command.CommandText =
			$@"SELECT *
				FROM {TableName}
			";

			using (var reader = command.ExecuteReader())
			{
				while (reader.Read())
				{
					var filename = reader.GetString(0);
					var data = (byte[])reader["Data"];
					var pngFile = Deserialize(data);
					yield return pngFile;
				}
			}
		}

		public PngFile ReadPngFile(string realFileName)
		{
			var command = connection.CreateCommand();
			command.CommandText =
			$@"SELECT *
				FROM {TableName}
				WHERE Filename = $id
			";
			command.Parameters.AddWithValue("$id", realFileName);

			using (var reader = command.ExecuteReader())
			{
				while (reader.Read())
				{
					var filename = reader.GetString(0);
					var data = (byte[])reader["Data"];
					var pngFile = Deserialize(data);
					return pngFile;
				}
			}
			return null;
		}

		public void WritePngFile(PngFile info)
		{
			var command = connection.CreateCommand();
			command.CommandText =
			$@"INSERT INTO {TableName}(Filename,FileExists,Data) VALUES ($id,$exists,$data)
			ON CONFLICT(Filename) DO UPDATE SET Data=$data,FileExists=$exists;
			";
			command.Parameters.AddWithValue("$id", info.Filename);
			command.Parameters.AddWithValue("$exists", info.Exists);
			command.Parameters.AddWithValue("$data", Serialize(info));
			command.Transaction = this.transaction;
			command.ExecuteNonQuery();
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
