using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SDMetaTool
{
    public class GenerationParams
    {
        private static readonly Regex sWhitespace = new Regex(@"\s+");

        public string Prompt { get; set; } = string.Empty;
        public string NegativePrompt { get; set; } = string.Empty;
        public string Params { get; set; } = string.Empty;

        public string NormalisedPrompt => sWhitespace.Replace(Prompt, " ");
        public string NormalisedNegativePrompt => sWhitespace.Replace(NegativePrompt, " ");

    }
}
