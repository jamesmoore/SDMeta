using SDMeta;
using SDMeta.Cache;
using SDMeta.Processors;
using SDMetaTool.Processors;
using System;
using System.Collections.Generic;
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
            using var sqliteDataSource = new SqliteDataSource(new DbPath(fileSystem, new DataPath(fileSystem)));
            var loader = new CachedPngFileLoader(fileSystem, new PngFileLoader(fileSystem), sqliteDataSource);
            var fileLister = new FileLister(fileSystem);
            return await Main(args, fileLister, sqliteDataSource, loader);
        }

        public static async Task<int> Main(
            string[] args,
            IFileLister fileLister,
            IPngFileDataSource pngFileDataSource,
            IPngFileLoader loader
            )
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            var pathArgument = new Argument<string>("path")
            {
                Description = "Path to mp3 directory",
            };

            var listCommand = new Command("list", "List sd metadata to csv.");
            listCommand.Arguments.Add(pathArgument);
            var outfileOption = new Option<string>("--outfile", "-o")
            {
                DefaultValueFactory = _ => "sdpnginfo.csv",
                Description = "Output file name.",
            };
            var distinctOption = new Option<bool>("--distinct", "-d") { 
                DefaultValueFactory = _ => false, 
                Description = "List distinct prompts with earliest file.",
            };
            listCommand.Options.Add(outfileOption);
            listCommand.Options.Add(distinctOption);
            listCommand.SetAction((p) => new CSVPngFileLister(new ImageDirs(p.GetValue(pathArgument)), fileLister, loader, p.GetValue(outfileOption), p.GetValue(distinctOption)).ProcessPngFiles());

            var infoCommand = new Command("info", "Info on files.");
            infoCommand.Arguments.Add(pathArgument);
            infoCommand.Options.Add(outfileOption);
            infoCommand.SetAction((p) => new SummaryInfo(new ImageDirs(p.GetValue(pathArgument)), fileLister, loader).ProcessPngFiles());

            var rescanCommand = new Command("rescan", "Rescan dir. No output.");
            rescanCommand.Arguments.Add(pathArgument);
            rescanCommand.SetAction((p) => new Rescan(new ImageDirs(p.GetValue(pathArgument)), fileLister, pngFileDataSource, loader).ProcessPngFiles());

            var parent = new RootCommand()
            {
               listCommand,
               infoCommand,
               rescanCommand,
            };

            var parseResult = parent.Parse(args);
            return await parseResult.InvokeAsync();
        }
    }

    public class ImageDirs(string path) : IImageDir
    {
        public IEnumerable<string> GetPath() => [path];
    }
}