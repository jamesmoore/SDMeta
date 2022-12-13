using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace SDMetaTool
{
    public partial class PngFile
    {
        private const string NegativePromptPrefix = "Negative prompt:";
        private const string SingleParameterRegexString = @"\s*([\w ]+):\s*(""(?:\\|\""|[^\""])+""|[^,]*)(?:,|$)";
        private const string MultipleParameterRegexString = "^(?:" + SingleParameterRegexString + "){3,}$";
        private const string ImageSize = @"^(\d+)x(\d+)$";
        private const string WildcardPrompt = @"Wildcard prompt: ""[\S\s]*"",\s";

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

            var re_imagesize = ImageSizeRegex();

            var withWildcardRemoved = WildcardPromptRegex().Replace(Parameters, "");

            var fullList = withWildcardRemoved.Trim().Split('\n').Select(p => p.Trim()).ToList();

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

            var paramsMatch = MultipleParameterRegex().Match(lastLine);

            var parametersLookup = Enumerable.Empty<string>().ToLookup(p => p, p => p);

            if (paramsMatch.Success)
            {
                parameters = lastLine;
                lines = lines.Take(lines.Count - 1).ToList();

                var parametersDecoded = SingleParameterRegex().Matches(lastLine);

                parametersLookup = parametersDecoded.Select(p => new { Key = p.Groups[1].Value, Value = p.Groups[2].Value }).ToLookup(p => p.Key, p => p.Value);
            }

            (string positive, string negative) = SplitPrompts(lines);

            return new GenerationParams()
            {
                Prompt = positive,
                NegativePrompt = negative,
                Params = parameters,
                Warnings = warningLineString,
                ModelHash = parametersLookup["Model hash"]?.FirstOrDefault(),
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


        [GeneratedRegex(SingleParameterRegexString)]
        private static partial Regex SingleParameterRegex();
        [GeneratedRegex(MultipleParameterRegexString)]
        private static partial Regex MultipleParameterRegex();
        [GeneratedRegex(ImageSize)]
        private static partial Regex ImageSizeRegex();
        [GeneratedRegex(WildcardPrompt)]
        private static partial Regex WildcardPromptRegex();
    }
}
