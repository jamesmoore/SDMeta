using NLog;
using SDMetaTool.Cache;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;

namespace SDMetaTool
{
	public class DirectoryProcessor : IDirectoryProcessor
	{
		private static readonly Logger logger = LogManager.GetCurrentClassLogger();
		private readonly IFileSystem fileSystem;

		public DirectoryProcessor(IFileSystem fileSystem)
		{
			this.fileSystem = fileSystem;
		}

		public int ProcessList(string path, IPngFileListProcessor processor, bool whatif = false)
		{
			while (path.EndsWith(fileSystem.Path.DirectorySeparatorChar))
			{
				path = path[0..^1];
			}

			if (fileSystem.Directory.Exists(path) == false)
			{
				logger.Error($"{path} does not exist");
				return 1;
			}

			var filetypes = new List<string>()
			{
				"*.png",
			};

			var files = filetypes.Select(p => fileSystem.Directory.GetFiles(path, p, System.IO.SearchOption.AllDirectories)).SelectMany(p => p).OrderBy(p => p).ToList();

			using (var cache = new JsonDataSource(fileSystem, whatif))
			{
				var loader = new CachedPngFileLoader(fileSystem, new PngFileLoader(fileSystem), cache);
				var tracks = files.Select(p => loader.GetPngFile(p)).Where(p => p != null).OrderBy(p => p.Filename).ToList();
				processor.ProcessPngFiles(tracks, path);
			}
			return 1;
		}
	}
}