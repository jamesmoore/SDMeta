namespace SDMetaUI.Models
{
    public class ImageFileViewModel
    {
        private readonly Func<string, string> func;
        private readonly string version;

        public ImageFileViewModel(string fileName, string fullPromptHash, string version, Func<string, string> func)
        {
            FileName = fileName;
            FullPromptHash = fullPromptHash;
            this.version = version;
            this.func = func;
            this.EncodedFileName = new(() => Base32Encode(this.FileName));
        }
        public string FileName { get; }
        public string ThumbnailUrl => $"/images/thumb/{EncodedFileName.Value}?v={version}";
        public string ImageUrl => $"/images/full/{EncodedFileName.Value}/{func(this.FileName)}?v={version}";
        public string FullPromptHash { get; }
        public IList<ImageFileViewModel>? SubItems { get; set; }

        private readonly Lazy<string> EncodedFileName;

        private static string Base32Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return Base32Encoding.ToString(plainTextBytes);
        }
    }
}
