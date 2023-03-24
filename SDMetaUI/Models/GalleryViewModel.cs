using SDMetaTool.Cache;
using SDMetaUI.Services;

namespace SDMetaUI.Models
{
	public class GalleryViewModel
	{
		public GalleryViewModel(
			IPngFileDataSource pngFileDataSource,
			PngFileViewModelBuilder pngFileViewModelBuilder)
		{
			this.filteredList = new FilteredList(pngFileDataSource, pngFileViewModelBuilder, PostFiltering);
			this.groupList = new FlatList(filteredList, PostGrouping);
		}

		private void PostGrouping()
		{
			this.Rows = groupList.GetChunks();
		}

		private IGroupList groupList;
		private readonly FilteredList filteredList;

		public PngFileViewModel? SelectedFile { get; set; }
		public PngFileViewModel? ExpandedFile => (this.groupList as IExpandable)?.ExpandedFile;

		public void Initialize()
		{
			filteredList.RunFilter();
		}

		public bool HasData => filteredList.Any();

		public int FilteredFileCount => filteredList.Count;

		public ModelSummaryViewModel ModelFilter
		{
			get => filteredList.ModelFilter;
			set => filteredList.ModelFilter = value;
		}

		public string Filter
		{
			get => filteredList.Filter;
			set => filteredList.Filter = value;
		}

		private void PostFiltering()
		{
			if (SelectedFile != null)
			{
				this.SelectedFile = filteredList.Get(SelectedFile.FileName);
			}
			this.groupList.RunGrouping();
		}

		public bool IsGrouped
		{
			get
			{
				return this.groupList is IExpandable;
			}
			set
			{
				var isGrouped = this.IsGrouped;
				if (isGrouped != value)
				{
					var chunks = this.groupList.ItemsPerRow;
					this.groupList = isGrouped == false ? new GroupedByPromptList(filteredList, PostGrouping) : new FlatList(filteredList, PostGrouping);
					this.groupList.ItemsPerRow = chunks;
				}
			}
		}

		private int width;
		public int Width
		{
			get { return width; }
			set
			{
				width = value;
				groupList.ItemsPerRow = this.CountPerRow();
			}
		}

		private int CountPerRow() => Math.Max((width - 17) / (ThumbnailService.ThumbnailSize + 8 * 2), 1);

		public void RemoveFile()
		{
			if (this.SelectedFile != null)
			{
				var next = this.groupList.GetNext(this.SelectedFile);
				if (next == this.SelectedFile) next = null;
				filteredList.Remove(this.SelectedFile);
				this.groupList.Remove(this.SelectedFile);
				this.SelectedFile = next;
			}
		}

		public void MovePrevious()
		{
			this.SelectedFile = this.groupList.GetPrevious(this.SelectedFile);
		}

		public void MoveNext()
		{
			this.SelectedFile = this.groupList.GetNext(this.SelectedFile);
		}

		public IList<GalleryRow> Rows { get; private set; }

		public void ToggleExpandedState(PngFileViewModel model)
		{
			(groupList as IExpandable)?.ToggleExpandedState(model);
		}
	}
}
