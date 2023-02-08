namespace SDMetaUI.Services
{
	public interface IThumbnailService
	{
		string GetOrGenerateThumbnail(string fullName);
		void Delete(string fullName);
	}
}