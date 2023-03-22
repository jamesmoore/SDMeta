namespace SDMetaUI.Models
{
	public interface IGroupList
	{
		void RunGrouping();
		IList<GalleryRow> GetChunks(int countPerRow);
		PngFileViewModel GetPrevious(PngFileViewModel current);
		PngFileViewModel GetNext(PngFileViewModel current);
		public void Remove(PngFileViewModel current);
		public PngFileViewModel? ExpandedFile { get; set; }
	}
}
