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
            var distinctOption = new Option<bool>(new string[] { "--distinct", "-d" }, () => false, "List distinct prompts with earliest file.");
            listCommand.AddOption(outfileOption);
            listCommand.AddOption(distinctOption);
            listCommand.SetHandler((string path, string outfile, bool distinct) => processor.ProcessList(path, new CSVPngFileLister(outfile, distinct)), pathArgument, outfileOption, distinctOption);

            var infoCommand = new Command("info", "Info on files.");
            infoCommand.AddArgument(pathArgument);
            infoCommand.AddOption(outfileOption);
            infoCommand.SetHandler((string path, string outfile) => processor.ProcessList(path, new SummaryInfo()), pathArgument, outfileOption);

            var parent = new RootCommand()
            {
               listCommand,
               infoCommand,
            };

            var result = await parent.InvokeAsync(args);
            return result;
        }
    }
}