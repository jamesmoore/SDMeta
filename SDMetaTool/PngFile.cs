using Coderanger.ImageInfo;
using Coderanger.ImageInfo.Decoders.Metadata;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;

namespace SDMetaTool
{
    public class PngFile
    {
        public string Filename { get; set; }
        public DateTime LastUpdated { get; set; }
        public string Parameters { get; set; }

        public GenerationParams GetParameters()
        {
            var re_param_code = new Regex(@"\s*([\w ]+):\s*(""(?:\\|\""|[^\""])+""|[^,]*)(?:,|$)");
            var re_params = new Regex("^(?:" + re_param_code.ToString() + "){3,}$");
            var re_imagesize = new Regex(@"^(\d+)x(\d+)$");

            var lines = Parameters.Trim().Split('\n').Select(p => p.Trim()).ToList();
            var lastLine = lines.Last();

            var parameters = string.Empty;

            if (re_params.Match(lastLine).Success)
            {
                parameters = lastLine;
                lines = lines.Take(lines.Count -1).ToList();
            }

            var positive = new List<string>();
            var negative = new List<string>();
            var negativeStart = lines.FirstOrDefault(p => p.StartsWith("Negative prompt:"));
            if(negativeStart != null)
            {
                var negativePosition = lines.IndexOf(negativeStart);
                positive = lines.Take(negativePosition).ToList();
                negative = lines.Skip(negativePosition).ToList();
            }
            else
            {
                positive = lines;
            }

            return new GenerationParams()
            {
                Prompt = string.Join('\n', positive).Trim(),
                NegativePrompt = string.Join('\n', negative).Substring(16).Trim(),
                Params = parameters,
            };
        }
    }
}
