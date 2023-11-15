using System;
using System.IO.Abstractions;
using System.Runtime.InteropServices;

namespace SDMeta.Cache
{
	public class DataPath(IFileSystem fileSystem)
	{
		public string GetPath() => RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ?
			"/var/lib/" + Application.ApplicationName.ToLower()
			: fileSystem.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Application.ApplicationName);

		internal void CreateIfMissing()
		{
			var path = GetPath();
			if (fileSystem.Directory.Exists(path) == false)
			{
				fileSystem.Directory.CreateDirectory(path);
			}
		}
	}
}
