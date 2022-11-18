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
            var processor = new DirectoryProcessor(fileSystem);
            return await Main(args, processor);
        }

        public static async Task<int> Main(string[] args, IDirectoryProcessor processor)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            var pathArgument = new Argument<string>("path", "Path to mp3 directory");

            var listCommand = new Command("list", "List sd metadata to csv.");
            listCommand.AddArgument(pathArgument);
            var outfileOption = new Option<string>(new string[] { "--outfile", "-o" }, () => "sdpnginfo.csv", "Output file name.");
            listCommand.AddOption(outfileOption);
            listCommand.SetHandler((string path, string outfile) => processor.ProcessList(path, new CSVPngFileLister(outfile)), pathArgument, outfileOption);

            var parent = new RootCommand()
            {
               listCommand,
            };

            var result = await parent.InvokeAsync(args);
            return result;
        }
    }
}