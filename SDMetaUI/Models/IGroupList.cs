namespace SDMetaUI.Models
{
	public interface IGroupList
	{
		void RunGrouping();
		IList<GalleryRow> GetChunks();
		ImageFileViewModel GetPrevious(ImageFileViewModel current);
		ImageFileViewModel GetNext(ImageFileViewModel current);
		void Remove(ImageFileViewModel current);
		public int ItemsPerRow { get; set; }
	}
}
