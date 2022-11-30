using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SDMetaTool
{
    public partial class PngFile
    {
        private const string NegativePromptPrefix = "Negative prompt:";
        private const string ParameterRegexString = @"\s*([\w ]+):\s*(""(?:\\|\""|[^\""])+""|[^,]*)(?:,|$)";
        private const string ParamsRegexString = "^(?:" + ParameterRegexString + "){3,}$";
        private const string ImageSize = @"^(\d+)x(\d+)$";

        public string Filename { get; set; }
        public DateTime LastUpdated { get; set; }
        public long Length { get; set; }
        public string Parameters { get; set; }

        public GenerationParams GetParameters()
        {
            if (string.IsNullOrWhiteSpace(Parameters))
            {
                return new GenerationParams();
            }

            var re_params = ParametersRegex();
            var re_imagesize = ImageSizeRegex();


            var fullList = Parameters.Trim().Split('\n').Select(p => p.Trim()).ToList();

            var warningLine = fullList.FirstOrDefault(p => p.StartsWith("Warning:"));

            var warningLineString = string.Empty;

            if (warningLine != null)
            {
                var warningStart = fullList.IndexOf(warningLine);
                var warningLines = fullList.Skip(warningStart).ToList();
                fullList = fullList.Take(warningStart).Where(p => string.IsNullOrWhiteSpace(p) == false).ToList();
                warningLineString = string.Join('\n', warningLines);
            }

            var lines = fullList;
            var lastLine = lines.Last();

            var parameters = string.Empty;

            if (re_params.Match(lastLine).Success)
            {
                parameters = lastLine;
                lines = lines.Take(lines.Count - 1).ToList();
            }

            (string positive, string negative) = SplitPrompts(lines);

            return new GenerationParams()
            {
                Prompt = positive,
                NegativePrompt = negative,
                Params = parameters,
                Warnings = warningLineString,
            };
        }

        private static (string positive, string negative) SplitPrompts(List<string> lines)
        {
            var positive = new List<string>();
            var negative = new List<string>();
            var negativeStart = lines.FirstOrDefault(p => p.StartsWith(NegativePromptPrefix));
            if (negativeStart != null)
            {
                var negativePosition = lines.IndexOf(negativeStart);
                positive = lines.Take(negativePosition).ToList();
                negative = lines.Skip(negativePosition).ToList();
                negative[0] = negative[0].Substring(NegativePromptPrefix.Length);
            }
            else
            {
                positive = lines;
            }

            var prompt = string.Join('\n', positive).Trim();
            var negativeString = string.Join('\n', negative).Trim();

            return (prompt, negativeString);
        }

        [GeneratedRegex(ParamsRegexString)]
        private static partial Regex ParametersRegex();
        [GeneratedRegex(ImageSize)]
        private static partial Regex ImageSizeRegex();
    }
}
