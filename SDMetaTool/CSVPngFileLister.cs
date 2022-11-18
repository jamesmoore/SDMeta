using CsvHelper;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

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
            csv.WriteRecords(tracks);
        }
    }
}
