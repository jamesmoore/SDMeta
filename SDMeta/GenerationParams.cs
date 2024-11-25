using System;

namespace SDMeta
{
	public class GenerationParams
	{
		public GenerationParams()
		{
			lazyPromptHash = new Lazy<string>(() => GetHash(Prompt));
			lazyNegativePromptHash = new Lazy<string>(() => GetHash(NegativePrompt));
		}

		private readonly Lazy<string> lazyPromptHash;
		private readonly Lazy<string> lazyNegativePromptHash;

		public string Prompt { get; set; }
		public string NegativePrompt { get; set; }
		public string ModelHash { get; set; }
		public string Model { get; set; }
		public string PromptHash => lazyPromptHash.Value;
		public string NegativePromptHash => lazyNegativePromptHash.Value;


		private string GetHash(string stringToHash)
		{
			if (stringToHash == null) return null;
			return stringToHash.NormalizeString().ComputeSHA256Hash();
		}
	}
}
