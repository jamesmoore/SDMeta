namespace SDMetaUI.Models
{
	public class FlatList : IGroupList
	{
		private readonly FilteredList FilteredList;

		public PngFileViewModel? ExpandedFile { get => null; set { } }

		public FlatList(FilteredList filteredList)
		{
			FilteredList = filteredList;
		}

		public void RunGrouping()
		{

		}

		public IList<GalleryRow> GetChunks(int countPerRow)
		{
			return FilteredList.FilteredFiles.Chunk(countPerRow).Select(p => new GalleryRow(p)).ToList();
		}

		public PngFileViewModel GetPrevious(PngFileViewModel current)
		{
			return this.FilteredList.FilteredFiles.GetPrevious(current);
		}

		public PngFileViewModel GetNext(PngFileViewModel current)
		{
			return this.FilteredList.FilteredFiles.GetNext(current);
		}

		public void Remove(PngFileViewModel current)
		{
			this.FilteredList.FilteredFiles.Remove(current);
		}
	}
}
