using Humanizer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDMetaTool
{
    internal class SummaryInfo : IPngFileListProcessor
    {
        public void ProcessPngFiles(IEnumerable<PngFile> tracks, string root)
        {
            var allFiles = tracks.Select(p => new { 
                PngFile = p, 
                Params = p.GetParameters() 
            }).ToList();
            var distinctPrompts = allFiles.Select(p => p.Params.Prompt).Distinct().ToList();

            Console.WriteLine($"{allFiles.Count} png files");
            Console.WriteLine($"{allFiles.Sum(p => p.PngFile.Length).Bytes().Humanize()} stored");
            Console.WriteLine($"{distinctPrompts.Count} distinct prompts");
        }
    }
}
