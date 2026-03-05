using SDMeta.Cache;
using SDMetaUI.Services;

namespace SDMetaUI.Models
{
	public class GalleryViewModel
	{
		public GalleryViewModel(
			IImageFileDataSource imageFileDataSource,
			ImageFileViewModelBuilder imageFileViewModelBuilder,
			IThumbnailService thumbnailService)
		{
			this.filteredList = new FilteredList(imageFileDataSource, imageFileViewModelBuilder, PostFiltering);
			this.groupList = new FlatList(filteredList, PostGrouping);
            this.imageFileDataSource = imageFileDataSource;
            this.thumbnailService = thumbnailService;
		}

		private void PostGrouping()
		{
			this.Rows = groupList.GetChunks();
		}

		private IGroupList groupList;
		private readonly FilteredList filteredList;
		private readonly IImageFileDataSource imageFileDataSource;
		private readonly IThumbnailService thumbnailService;

		public ImageFileViewModel? SelectedFile { get; set; }
		public ImageFileViewModel? ExpandedFile => (this.groupList as IExpandable)?.ExpandedFile;

		public void Initialize()
		{
			filteredList.RunFilter();
		}

		public bool HasData { get; private set; }

		public int FilteredFileCount => filteredList.Count;

		public ModelSummaryViewModel ModelFilter
		{
			get => filteredList.ModelFilter;
			set => filteredList.ModelFilter = value;
		}

		public string? Filter
		{
			get => filteredList.Filter;
			set => filteredList.Filter = value;
		}

        public QuerySortBy SortBy
        {
            get => filteredList.SortBy;
            set => filteredList.SortBy = value;
        }

        public bool FilterError => filteredList.FilterError;

		private void PostFiltering()
		{
			if (SelectedFile != null)
			{
				this.SelectedFile = filteredList.Get(SelectedFile.FileName);
			}
			this.groupList.RunGrouping();
			this.HasData = true;
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

		public bool AutoRescan { get; set; }

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
				var filename = this.SelectedFile.FileName;
				this.thumbnailService.Delete(filename);
				File.Delete(filename);
				var original = imageFileDataSource.ReadImageFile(filename);
				if (original != null)
				{
					original.Exists = false;
					imageFileDataSource.WriteImageFile(original);
				}

				var next = this.groupList.GetNext(this.SelectedFile);
				if (next == this.SelectedFile) next = null;
				filteredList.Remove(this.SelectedFile);
				this.groupList.Remove(this.SelectedFile);
				this.SelectedFile = next;
			}
		}

		public void MovePrevious()
		{
			if (this.SelectedFile != null)
			{
				this.SelectedFile = this.groupList.GetPrevious(this.SelectedFile);
			}
		}

		public void MoveNext()
		{
			if (this.SelectedFile != null)
			{
				this.SelectedFile = this.groupList.GetNext(this.SelectedFile);
			}
		}

		public IList<GalleryRow> Rows { get; private set; } = new List<GalleryRow>();

		public void ToggleExpandedState(ImageFileViewModel model)
		{
			(groupList as IExpandable)?.ToggleExpandedState(model);
		}

		public IList<ModelSummaryViewModel> GetModelsList()
		{
			var modelsList = this.imageFileDataSource.GetModelSummaryList().Select((p, i) => new ModelSummaryViewModel(p, i + 1)).ToList();
			modelsList.Insert(0, ModelSummaryViewModel.AllModels);
			return modelsList;
		}
	}
}
