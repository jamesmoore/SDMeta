using System;
using System.IO.Abstractions;

namespace SDMeta.Cache
{
	public class CachePath(IFileSystem fileSystem)
	{
		public string GetPath() => fileSystem.Path.Combine(new DataPath(fileSystem).GetPath(), "cache.json");
	}
}
