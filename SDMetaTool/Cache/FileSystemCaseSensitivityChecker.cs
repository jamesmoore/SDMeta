using NLog;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDMetaTool.Cache
{
	public class FileSystemCaseSensitivityChecker : IFileSystemCaseSensitivityChecker
	{
		private bool? firstCheck;
		private readonly IFileSystem fileSystem;
		private static readonly Logger logger = LogManager.GetCurrentClassLogger();
		public FileSystemCaseSensitivityChecker(IFileSystem fileSystem)
		{
			this.fileSystem = fileSystem;
		}

		public bool? IsCaseSensitive(string path)
		{
			if (firstCheck == null)
			{
				if (fileSystem.File.Exists(path))
				{
					firstCheck = fileSystem.File.Exists(path.ToLower()) && fileSystem.File.Exists(path.ToUpper());
					logger.Debug("File system case sensitivity determined to be " + firstCheck);
				}
			}
			return firstCheck;
		}
	}
}
