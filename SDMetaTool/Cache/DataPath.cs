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
			fileSystem.Path.DirectorySeparatorChar + "SDMetaTool"
			: fileSystem.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SDMetaTool");
	}
}
