namespace SDMetaUI.Models
{
	public interface IExpandable
	{
		PngFileViewModel? ExpandedFile { get; }
		void ToggleExpandedState(PngFileViewModel model);
	}
}
