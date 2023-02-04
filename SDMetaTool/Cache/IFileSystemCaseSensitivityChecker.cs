namespace SDMetaTool.Cache
{
	public interface IFileSystemCaseSensitivityChecker
	{
		bool? IsCaseSensitive(string path);
	}
}