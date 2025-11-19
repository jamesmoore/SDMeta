using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;

namespace SDMeta
{
	public class FileLister(IFileSystem fileSystem, ILogger<FileLister> logger) : IFileLister
	{
		public IEnumerable<string> GetList(string path)
        {
            while (path.EndsWith(fileSystem.Path.DirectorySeparatorChar))
            {
                path = path[0..^1];
            }

            if (fileSystem.Directory.Exists(path) == false)
            {
                logger.LogError($"{path} does not exist");
                return Enumerable.Empty<string>();
            }

            var filetypes = new List<string>()
            {
                "*.png",
                "*.jpg",
            };

            var files = filetypes.Select(p => GetFileList(path, p)).SelectMany(p => p).OrderBy(p => p).ToList();

            return files;

        }

        private string[] GetFileList(string path, string p)
        {
            try
            {
                return fileSystem.Directory.GetFiles(path, p,
                    new EnumerationOptions
                    {
                        IgnoreInaccessible = true,
                        RecurseSubdirectories = true,
                    } );
            }
            catch (Exception ex)
            {
                logger.LogWarning("Unable to scan directory " + path);
                return [];
            }
        }
    }
}