using System.Reflection;
using System.Text.RegularExpressions;

namespace SDMetaTool
{
    public partial class GenerationParams
    {
        private static readonly Regex sWhitespace = WhitespaceRegex();

        public string Prompt { get; set; } = string.Empty;
        public string NegativePrompt { get; set; } = string.Empty;
        public string Params { get; set; } = string.Empty;
        public string Warnings { get; set; } = string.Empty;


        public string Steps { get; set; } = string.Empty;
        public string Sampler { get; set; } = string.Empty;
        public string CFGScale { get; set; } = string.Empty;
        public string Seed { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string ModelHash { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string ClipSkip { get; set; } = string.Empty;


        public string NormalisedPrompt => sWhitespace.Replace(Prompt, " ");
        public string NormalisedNegativePrompt => sWhitespace.Replace(NegativePrompt, " ");

        [GeneratedRegex(@"\s+")]
        private static partial Regex WhitespaceRegex();
    }
}
