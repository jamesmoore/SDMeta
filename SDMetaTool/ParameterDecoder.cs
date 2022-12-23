using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace SDMetaTool
{
    public partial class ParameterDecoder
    {
        private const string NegativePromptPrefix = "Negative prompt:";
        private const string SingleParameterRegexString = @"\s*([\w ]+):\s*(""(?:\\|\""|[^\""])+""|[^,]*)(?:,|$)";
        private const string MultipleParameterRegexString = "^(?:" + SingleParameterRegexString + "){3,}$";
        private const string ImageSize = @"^(\d+)x(\d+)$";
        private const string WildcardPrompt = @",?\s?Wildcard prompt: ""[\S\s]*""";

        public GenerationParams GetParameters(string _parameters)
        {
            if (string.IsNullOrWhiteSpace(_parameters))
            {
                return new GenerationParams();
            }

            var re_imagesize = ImageSizeRegex();

            var withWildcardRemoved = WildcardPromptRegex().Replace(_parameters, "");

            var fullList = withWildcardRemoved.Trim().Split('\n').Select(p => p.Trim()).ToList();

            var warningLine = fullList.FirstOrDefault(p => p.StartsWith("Warning:"));

            var warningLineString = null as string;

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
                Steps = parametersLookup["Steps"]?.FirstOrDefault(),
                Sampler = parametersLookup["Sampler"]?.FirstOrDefault(),
                CFGScale = parametersLookup["CFG scale"]?.FirstOrDefault(),
                Size = parametersLookup["Size"]?.FirstOrDefault(),
                ClipSkip = parametersLookup["Clip skip"]?.FirstOrDefault(),
                Seed = parametersLookup["Seed"]?.FirstOrDefault(),
                Model = parametersLookup["Model"]?.FirstOrDefault(),
                DenoisingStrength = parametersLookup["Denoising strength"]?.FirstOrDefault(),
                PromptHash = ComputeSha256Hash(WhitespaceRegex().Replace(positive, " ").ToLower()),
                NegativePromptHash = ComputeSha256Hash(WhitespaceRegex().Replace(negative, " ").ToLower()),
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
        [GeneratedRegex(@"\s+")]
        private static partial Regex WhitespaceRegex();

        static string ComputeSha256Hash(string rawData)
        {
            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
