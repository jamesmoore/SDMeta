using System;
using System.IO.Abstractions;

namespace SDMetaTool.Cache
{
	public class DbPath
	{
		private readonly IFileSystem fileSystem;
        private readonly DataPath dataPath;

        public DbPath(IFileSystem fileSystem, DataPath dataPath)
		{
			this.fileSystem = fileSystem;
            this.dataPath = dataPath;
        }

		public string GetPath() => fileSystem.Path.Combine(dataPath.GetPath(), "cacheFTS.db");

		internal void CreateIfMissing()
		{
			dataPath.CreateIfMissing();
		}
	}
}
