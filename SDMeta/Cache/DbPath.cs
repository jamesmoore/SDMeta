using System;
using System.IO.Abstractions;

namespace SDMeta.Cache
{
	public class DbPath(IFileSystem fileSystem, DataPath dataPath)
	{
		public string GetPath() => fileSystem.Path.Combine(dataPath.GetPath(), "cacheFTS.db");

		internal void CreateIfMissing()
		{
			dataPath.CreateIfMissing();
		}
	}
}
