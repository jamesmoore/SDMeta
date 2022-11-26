using CsvHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace SDMetaTool
{
    class CSVPngFileLister : IPngFileListProcessor
    {
        private readonly string outfile;

        public CSVPngFileLister(string outfile)
        {
            this.outfile = outfile;
        }

        public void ProcessPngFiles(IEnumerable<PngFile> tracks, string root)
        {
            using var writer = new StreamWriter(outfile);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteRecords(tracks.Select(ToCSV));
        }

        private static CSVEntry ToCSV(PngFile p)
        {
            var generationParams = p.GetParameters();
            return new CSVEntry()
            {
                Filename = p.Filename,
                LastUpdated = p.LastUpdated,
                Length = p.Length,
                Prompt = generationParams.Prompt,
                NegativePrompt = generationParams.NegativePrompt,
                Parameters = generationParams.Params,
            };
        }

        private class CSVEntry
        {
            public string Filename { get; set; }
            public DateTime LastUpdated { get; set; }
            public long Length { get; set; }
            public string Prompt { get; set; }
            public string NegativePrompt { get; set; }
            public string Parameters { get; set; }

        }
    }
}
