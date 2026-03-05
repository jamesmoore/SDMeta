using SDMeta;

namespace SDMetaUI
{
    public class ImageDir(IConfiguration configuration) : IImageDir
    {
        private readonly IConfiguration configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        private static readonly string[] keys = ["ImageDir", .. Enumerable.Range(0, 10).Select(p => $"ImageDir:{p}")];

        public IEnumerable<string> GetPath()
        {
            var imagedir = keys.Select(p => configuration[p]).Where(p => p != null).Select(p => p!);
            return imagedir.ToList();
        }
    }
}
