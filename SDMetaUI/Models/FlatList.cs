namespace SDMetaUI.Models
{
	public class FlatList : IGroupList
	{
		private readonly FilteredList filteredList;
		private readonly Action postGroupingAction;

		public FlatList(
			FilteredList filteredList,
			Action postGroupingAction
			)
		{
			this.filteredList = filteredList;
			this.postGroupingAction = postGroupingAction;
		}

		public void RunGrouping() => postGroupingAction();

		public IList<GalleryRow> GetChunks() => filteredList.Chunk(countPerRow).Select(p => new GalleryRow(p)).ToList();

		public PngFileViewModel GetPrevious(PngFileViewModel current) => this.filteredList.GetPrevious(current);

		public PngFileViewModel GetNext(PngFileViewModel current) => this.filteredList.GetNext(current);

		public void Remove(PngFileViewModel current)
		{
			this.filteredList.Remove(current);
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
