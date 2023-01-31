using System;

namespace SDMetaTool
{
    public partial class PngFile
    {
        public string Filename { get; set; }
        public DateTime LastUpdated { get; set; }
        public long Length { get; set; }
        public GenerationParams Parameters { get; set; }

        /// <summary>
        /// Whether the file exists on the most recent scan
        /// </summary>
        public bool Exists { get; set; }
    }
}
