using SDMetaTool.Cache;
using SDMetaTool.Processors;
using System;
using System.CommandLine;
using System.IO.Abstractions;
using System.Threading.Tasks;

namespace SDMetaTool
{
    public class Program
    {
        static async Task<int> Main(string[] args)
        {
            var fileSystem = new FileSystem();
			using var pngfileDataSource = new JsonDataSource(fileSystem);
			var loader = new CachedPngFileLoader(fileSystem, new PngFileLoader(fileSystem), pngfileDataSource);
			var fileLister = new FileLister(fileSystem);
			return await Main(args, fileLister, pngfileDataSource, loader);
		}

        public static async Task<int> Main(
            string[] args, 
            IFileLister fileLister, 
            IPngFileDataSource pngFileDataSource,
			IPngFileLoader loader
			)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            var pathArgument = new Argument<string>("path", "Path to mp3 directory");

            var listCommand = new Command("list", "List sd metadata to csv.");
            listCommand.AddArgument(pathArgument);
            var outfileOption = new Option<string>(new string[] { "--outfile", "-o" }, () => "sdpnginfo.csv", "Output file name.");
            var distinctOption = new Option<bool>(new string[] { "--distinct", "-d" }, () => false, "List distinct prompts with earliest file.");
            listCommand.AddOption(outfileOption);
            listCommand.AddOption(distinctOption);
            listCommand.SetHandler((string path, string outfile, bool distinct) => new CSVPngFileLister(fileLister, loader, outfile, distinct).ProcessPngFiles(path), pathArgument, outfileOption, distinctOption);

            var infoCommand = new Command("info", "Info on files.");
            infoCommand.AddArgument(pathArgument);
            infoCommand.AddOption(outfileOption);
            infoCommand.SetHandler((string path, string outfile) => new SummaryInfo(fileLister, loader).ProcessPngFiles(path), pathArgument, outfileOption);

			var rescanCommand = new Command("rescan", "Rescan dir. No output.");
			rescanCommand.AddArgument(pathArgument);
			rescanCommand.SetHandler((string path) => new Rescan(fileLister, pngFileDataSource, loader).ProcessPngFiles(path), pathArgument);

			var parent = new RootCommand()
            {
               listCommand,
               infoCommand,
			   rescanCommand,
			};

            var result = await parent.InvokeAsync(args);
            return result;
        }
    }
}