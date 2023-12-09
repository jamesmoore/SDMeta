using SDMeta;

namespace SDMetaUI
{
	public class ImageDir(IConfiguration configuration) : IImageDir
	{
		private readonly IConfiguration configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

		public IEnumerable<string> GetPath()
		{
			var keys = new List<string>() { "ImageDir" }.Concat(Enumerable.Range(0, 10).Select(p => $"ImageDir:{p}"));
			var imagedir = keys.Select(p => configuration[p]).Where(p => p != null);
			return imagedir.ToList();
		}
	}
}
