using CsvHelper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace SDMetaTool
{
    class CSVPngFileLister : IPngFileListProcessor
    {
        private readonly string outfile;
        private readonly bool distinct;

        public CSVPngFileLister(string outfile, bool distinct)
        {
            this.outfile = outfile;
            this.distinct = distinct;
        }

        public void ProcessPngFiles(IEnumerable<PngFile> tracks, string root)
        {
            using var writer = new StreamWriter(outfile);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            var csvs = distinct ? GetCSVDistinct(tracks) : tracks.OrderBy(p => p.LastUpdated).Select(p => ToCSV(p, 1));
            csv.WriteRecords(csvs);
        }

        private IEnumerable<CSVEntry> GetCSVDistinct(IEnumerable<PngFile> tracks)
        {
            var allFiles = tracks.Select(p => new
            {
                PngFile = p,
                Params = p.GetParameters()
            }).ToList();

            var groupedBy = allFiles.GroupBy(p => new
            {
                p.Params.NormalisedPrompt,
                p.Params.NormalisedNegativePrompt
            });

            var tracks2 = groupedBy.Select(p => ToCSV(p.OrderBy(p => p.PngFile.LastUpdated).First().PngFile, p.Count())).OrderBy(p => p.LastUpdated);
            return tracks2;
        }

        private static CSVEntry ToCSV(PngFile p, int count)
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
                Warnings = generationParams.Warnings, 
                Count = count
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
            public string Warnings { get; set; }
            public int Count { get; set; }

        }
    }
}
