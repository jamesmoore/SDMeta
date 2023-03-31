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

		public string GetPath() => fileSystem.Path.Combine(new DataPath(fileSystem).GetPath(), "cacheFTS.db");
	}
}
