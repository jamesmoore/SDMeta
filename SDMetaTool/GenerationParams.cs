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
		public string BatchSize { get; set; }
		public string BatchPos { get; set; }
		public string FaceRestoration { get; set; }
		public string Eta { get; set; }
		public string FirstPassSize { get; set; }
		public string ENSD { get; set; }
		public string Hypernet { get; set; }
        public string HypernetHash { get; set; }
        public string HypernetStrength { get; set; }
		public string MaskBlur { get; set; }
		public string VariationSeed { get; set; }
		public string VariationSeedStrength { get; set; }
		public string SeedResizeFrom { get; set; }
		public string HiresResize { get; set; }
		public string HiresUpscaler { get; set; }

		public string PromptHash { get; set; }
        public string NegativePromptHash { get; set; }
	}
}
