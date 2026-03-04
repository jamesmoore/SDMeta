namespace SDMetaUI.Models
{
	public class FlatList(
		FilteredList filteredList,
		Action postGroupingAction
			) : IGroupList
	{
		public void RunGrouping() => postGroupingAction();

		public IList<GalleryRow> GetChunks() => filteredList.Chunk(countPerRow).Select(p => new GalleryRow(p)).ToList();

		public ImageFileViewModel GetPrevious(ImageFileViewModel current) => filteredList.GetPrevious(current);

		public ImageFileViewModel GetNext(ImageFileViewModel current) => filteredList.GetNext(current);

		public void Remove(ImageFileViewModel current)
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
