using System.Collections.Generic;
using System.Linq;

namespace SDMetaTool
{
    public partial class GenerationParams
    {
        public string Prompt { get; set; }
        public string NegativePrompt { get; set; }
        public string Params { get; set; }
        public string Warnings { get; set; }
 
        public string ModelHash { get; set; }
        public string Model { get; set; }

		public string PromptHash { get; set; }
        public string NegativePromptHash { get; set; }

		public string GetFullPrompt()
        {
            var prompts = new List<string>()
            {
                this.Prompt,
                string.IsNullOrWhiteSpace(this.NegativePrompt) ? string.Empty : "Negative prompt: " + this.NegativePrompt,
                this.Params
            };

            var combined = string.Join("\r\n", prompts.Where(p => string.IsNullOrWhiteSpace(p) == false).ToArray());
            return combined;
        }
	}
}
