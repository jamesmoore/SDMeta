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

        public string NormalisedPrompt => sWhitespace.Replace(Prompt, " ");
        public string NormalisedNegativePrompt => sWhitespace.Replace(NegativePrompt, " ");

        [GeneratedRegex(@"\s+")]
        private static partial Regex WhitespaceRegex();
    }
}
