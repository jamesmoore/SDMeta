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
			this.filteredList = new FilteredList(pngFileDataSource, pngFileViewModelBuilder);
			this.groupList = new FlatList(filteredList);
		}

		private IGroupList groupList;
		private readonly FilteredList filteredList;

		public PngFileViewModel? SelectedFile { get; set; }
		public PngFileViewModel? ExpandedFile
		{
			get => this.groupList.ExpandedFile;
			set => this.groupList.ExpandedFile = value;
		}

		public void Initialize()
		{
			filteredList.RunFilter();
			PostFiltering();
		}

		public bool HasData => filteredList.FilteredFiles != null;

		public int FilteredFileCount => filteredList.FilteredFiles.Count;

		public ModelSummaryViewModel ModelFilter
		{
			get
			{
				return filteredList.ModelFilter;
			}
			set
			{
				filteredList.ModelFilter = value;
				PostFiltering();
			}
		}

		public string Filter
		{
			get
			{
				return filteredList.Filter;
			}
			set
			{
				filteredList.Filter = value;
				PostFiltering();
			}
		}

		private void PostFiltering()
		{
			if (SelectedFile != null)
			{
				this.SelectedFile = filteredList.Get(SelectedFile.FileName);
			}
			this.groupList.RunGrouping();
			this.Rows = this.groupList.GetChunks(CountPerRow());
		}

		private bool isGrouped;

		public bool IsGrouped
		{
			get
			{
				return isGrouped;
			}
			set
			{
				if (isGrouped != value)
				{
					isGrouped = value;
					this.groupList = isGrouped ? new GroupedByPromptList(filteredList) : new FlatList(filteredList);
					this.groupList.RunGrouping();
					this.Rows = groupList.GetChunks(this.CountPerRow());
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
				if (this.HasData)
				{
					this.Rows = groupList.GetChunks(this.CountPerRow());
				}
			}
		}

		private int CountPerRow() => Math.Max((width - 17) / (ThumbnailService.ThumbnailSize + 8 * 2), 1);

		public void RemoveFile()
		{
			if (this.SelectedFile != null)
			{
				var next = this.groupList.GetNext(this.SelectedFile);
				if (next == this.SelectedFile) next = null;
				filteredList.FilteredFiles.Remove(this.SelectedFile);
				if (this.SelectedFile == this.ExpandedFile && this.ExpandedFile.SubItems?.Count > 1)
				{
					this.ExpandedFile.SubItems.Remove(SelectedFile);
					var replacement = this.ExpandedFile.SubItems.First();
					replacement.SubItems = this.ExpandedFile.SubItems;
					groupList.Replace(this.ExpandedFile, replacement);
					this.ExpandedFile = replacement;
				}
				else
				{
					this.groupList.Remove(this.SelectedFile);
					this.ExpandedFile?.SubItems?.Remove(this.SelectedFile);
				}
				this.Rows = groupList.GetChunks(this.CountPerRow());
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
			this.ExpandedFile = model == this.ExpandedFile ? null : model;
			this.Rows = groupList.GetChunks(this.CountPerRow());
		}
	}

	public static class ListExtensions
	{
		public static void Replace<T>(this IList<T> list, T oldItem, T newItem)
		{
			var oldItemIndex = list.IndexOf(oldItem);
			if (oldItemIndex >= 0)
			{
				list[oldItemIndex] = newItem;
			}
		}
	}
}
