namespace SDMetaUI.Models
{
	public interface IGroupList
	{
		void RunGrouping();
		IList<GalleryRow> GetChunks();
		PngFileViewModel GetPrevious(PngFileViewModel current);
		PngFileViewModel GetNext(PngFileViewModel current);
		void Remove(PngFileViewModel current);
		public int ItemsPerRow { get; set; }
	}
}
