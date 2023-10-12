using System;
using System.IO.Abstractions;
using System.Runtime.InteropServices;

namespace SDMetaTool.Cache
{
	public class DataPath
	{
		private readonly IFileSystem fileSystem;

		public DataPath(IFileSystem fileSystem)
		{
			this.fileSystem = fileSystem;
		}

		public string GetPath() => (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) ? 
			"/var/lib/" + Application.ApplicationName.ToLower()
			: fileSystem.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Application.ApplicationName);

		internal void CreateIfMissing()
		{
			var path = GetPath();
			if(fileSystem.Directory.Exists(path) == false)
			{
				fileSystem.Directory.CreateDirectory(path);
			}
		}
	}
}
