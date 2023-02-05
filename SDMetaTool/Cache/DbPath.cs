using System;
using System.IO;
using System.IO.Abstractions;

namespace SDMetaTool.Cache
{
	public class DbPath
	{
		private readonly IFileSystem fileSystem;

		public DbPath(IFileSystem fileSystem)
		{
			this.fileSystem = fileSystem;
		}

		public string GetPath() => fileSystem.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SDMetaTool", "cache.db");
	}
}
