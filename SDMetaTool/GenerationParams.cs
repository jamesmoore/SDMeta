namespace SDMetaTool
{
    public partial class GenerationParams
    {
        public string Prompt { get; set; }
        public string NegativePrompt { get; set; }
        public string Params { get; set; }
        public string Warnings { get; set; }


        public string Steps { get; set; }
        public string Sampler { get; set; }
        public string CFGScale { get; set; }
        public string Seed { get; set; }
        public string Size { get; set; }
        public string ModelHash { get; set; }
        public string Model { get; set; }
        public string ClipSkip { get; set; }
        public string DenoisingStrength { get; set; }


        public string PromptHash { get; set; }
        public string NegativePromptHash { get; set; }
    }
}
