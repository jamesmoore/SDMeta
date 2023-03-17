namespace SDMetaUI.Models
{
	public class PngFileViewModel
	{
		private readonly Func<string, string> func;

		public PngFileViewModel(string fileName, string fullPromptHash, Func<string, string> func)
		{
			FileName = fileName;
			FullPromptHash = fullPromptHash;
			this.func = func;
			this.EncodedFileName = new(() => Base32Encode(this.FileName));
		}
		public string FileName { get; }
		public string ThumbnailUrl => $"/images/thumb/{EncodedFileName.Value}";
		public string ImageUrl => $"/images/full/{EncodedFileName.Value}/{func(this.FileName)}";
		public string FullPromptHash { get; }
		public IList<PngFileViewModel> SubItems { get; set; }

		private readonly Lazy<string> EncodedFileName; 

		private static string Base32Encode(string plainText)
		{
			var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
			return Base32Encoding.ToString(plainTextBytes);
		}
	}
}
