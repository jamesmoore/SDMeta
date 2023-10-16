using System;
using System.IO.Abstractions;

namespace SDMeta.Cache
{
	public class CachePath
	{
		private readonly IFileSystem fileSystem;

		public CachePath(IFileSystem fileSystem)
		{
			this.fileSystem = fileSystem;
		}

		public string GetPath() => fileSystem.Path.Combine(new DataPath(fileSystem).GetPath(), "cache.json");
	}
}
