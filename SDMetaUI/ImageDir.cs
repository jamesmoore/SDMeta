namespace SDMetaUI
{
	public class ImageDir(IConfiguration configuration)
	{
		private readonly IConfiguration configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

		public string GetPath()
		{
			return configuration["ImageDir"];
		}
	}
}
