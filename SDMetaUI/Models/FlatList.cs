namespace SDMetaUI.Models
{
	public class FlatList(
		FilteredList filteredList,
		Action postGroupingAction
			) : IGroupList
	{
		public void RunGrouping() => postGroupingAction();

		public IList<GalleryRow> GetChunks() => filteredList.Chunk(countPerRow).Select(p => new GalleryRow(p)).ToList();

		public PngFileViewModel GetPrevious(PngFileViewModel current) => filteredList.GetPrevious(current);

		public PngFileViewModel GetNext(PngFileViewModel current) => filteredList.GetNext(current);

		public void Remove(PngFileViewModel current)
		{
			filteredList.Remove(current);
			postGroupingAction();
		}

		private int countPerRow = 1;
		public int ItemsPerRow
		{
			get => countPerRow;
			set
			{
				if (countPerRow != value)
				{
					countPerRow = value;
					postGroupingAction();
				}
			}
		}
	}
}
