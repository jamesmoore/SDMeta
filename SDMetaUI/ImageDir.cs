namespace SDMetaUI
{
	public class ImageDir
	{
		private readonly IConfiguration configuration;

		public ImageDir(IConfiguration configuration)
		{
			this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
		}

		public string GetPath()
		{
			return configuration["ImageDir"];
		}
	}
}
