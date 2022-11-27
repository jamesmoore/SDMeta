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

        public string Prompt { get; set; }
        public string NegativePrompt { get; set; }
        public string Params { get; set; }

        public string NormalisedPrompt => sWhitespace.Replace(Prompt, " ");
        public string NormalisedNegativePrompt => sWhitespace.Replace(Prompt, " ");

    }
}
