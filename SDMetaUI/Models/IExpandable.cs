namespace SDMetaUI.Models
{
	public interface IExpandable
	{
		ImageFileViewModel? ExpandedFile { get; }
		void ToggleExpandedState(ImageFileViewModel model);
	}
}
