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
			this.pngFileDataSource = pngFileDataSource;
			this.pngFileViewModelBuilder = pngFileViewModelBuilder;
		}

		private readonly IPngFileDataSource pngFileDataSource;
		private readonly PngFileViewModelBuilder pngFileViewModelBuilder;
		private IList<PngFileViewModel> filteredFiles = null;
		private IList<PngFileViewModel> groupedFiles = null;
		public PngFileViewModel SelectedFile { get; set; }
		public PngFileViewModel ExpandedFile { get; private set; }
		private ModelSummaryViewModel modelFilter;

		private IDictionary<string, List<PngFileViewModel>> promptGroups = null;

		public ModelSummaryViewModel ModelFilter
		{
			get
			{
				return modelFilter;
			}
			set
			{
				if (modelFilter != value)
				{
					modelFilter = value;
					RunFilter();
				}
			}
		}

		public void Initialize()
		{
			RunFilter();
			if (SelectedFile != null)
			{
				this.SelectedFile = filteredFiles.FirstOrDefault(p => p.FileName == SelectedFile.FileName);
			}
			if (ExpandedFile != null)
			{
				this.ExpandedFile = filteredFiles.FirstOrDefault(p => p.FileName == ExpandedFile.FileName);
			}
		}

		public bool HasData => filteredFiles != null;

		public int FilteredFileCount => filteredFiles.Count;

		private string filter;

		public string Filter
		{
			get
			{
				return filter;
			}
			set
			{
				if (filter != value)
				{
					filter = value;
					RunFilter();
				}
			}
		}

		private void RunFilter()
		{
			var queryParams = new QueryParams()
			{
				Filter = this.filter,
				ModelFilter = this.modelFilter == null ? null : new ModelFilter()
				{
					Model = this.modelFilter.Model,
					ModelHash = this.modelFilter.ModelHash,
				}
			};
			filteredFiles = pngFileDataSource.Query(queryParams).OrderByDescending(p => p.LastUpdated).Select(p => pngFileViewModelBuilder.BuildModel(p)).ToList();

			if (this.ExpandedFile != null)
			{
				ExpandedFile = this.filteredFiles.FirstOrDefault(p => p.FileName == ExpandedFile.FileName);
			}

			this.promptGroups = filteredFiles.GroupBy(p => p.FullPromptHash).ToDictionary(p => p.Key, p => p.ToList());

			RunGrouping();
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
					if (isGrouped == false)
					{
						this.ExpandedFile = null;
					}
					RunGrouping();
				}
			}
		}

		private void RunGrouping()
		{
			if (isGrouped)
			{
				groupedFiles = this.promptGroups.Select(p => p.Value.First()).ToList();
				foreach (var file in groupedFiles)
				{
					file.SubItems = this.promptGroups[file.FullPromptHash];
				}
			}
			else
			{
				groupedFiles = filteredFiles;
			}
			RunChunking();
		}

		private int width;
		public int Width
		{
			get { return width; }
			set
			{
				width = value;
				RunChunking();
			}
		}

		private void RunChunking()
		{
			if (width > 0 && groupedFiles != null)
			{
				var countPerRow = CountPerRow();

				if (this.IsGrouped && (this.ExpandedFile?.SubItems?.Any() ?? false))
				{
					var position = this.groupedFiles.TakeWhile(p => p != this.ExpandedFile).Count() + 1;
					var modulo = position % countPerRow;
					if (modulo > 0)
					{
						position += (countPerRow - modulo);
					}
					var before = this.groupedFiles.Take(position).Chunk(countPerRow).Select(p => new GalleryRow(p));
					var expandedChunks = this.ExpandedFile.SubItems.Chunk(countPerRow).ToList();
					var middle = expandedChunks.Select((p, i) => new GalleryRow(p, true, i == 0, i == expandedChunks.Count - 1));
					var after = this.groupedFiles.Skip(position).Chunk(countPerRow).Select(p => new GalleryRow(p));
					this.Rows = before.Concat(middle).Concat(after).ToList();
				}
				else
				{
					this.Rows = groupedFiles.Chunk(countPerRow).Select(p => new GalleryRow(p)).ToList();
				}
			}
		}

		private int CountPerRow() => (width - 17) / (ThumbnailService.ThumbnailSize + 8 * 2);

		public void RemoveFile()
		{
			if (this.SelectedFile != null)
			{
				var next = this.GetNext();
				if (next == this.SelectedFile) next = null;
				if (this.SelectedFile == this.ExpandedFile && this.ExpandedFile.SubItems?.Count > 1)
				{
					this.ExpandedFile.SubItems.Remove(SelectedFile);
					var replacement = this.ExpandedFile.SubItems.First();
					replacement.SubItems = this.ExpandedFile.SubItems;
					filteredFiles.Replace(this.ExpandedFile, replacement);
					groupedFiles.Replace(this.ExpandedFile, replacement);
					this.ExpandedFile = replacement;
				}
				else
				{
					filteredFiles.Remove(this.SelectedFile);
					groupedFiles.Remove(this.SelectedFile);
					this.ExpandedFile?.SubItems?.Remove(this.SelectedFile);
				}
				RunChunking();
				this.SelectedFile = next;
			}
		}

		public void MovePrevious()
		{
			this.SelectedFile = GetPrevious();
		}

		private PngFileViewModel GetPrevious()
		{
			var sourceList = this.isGrouped ? this.promptGroups[this.SelectedFile.FullPromptHash] : filteredFiles;
			return sourceList.GetPrevious(this.SelectedFile);
		}

		public void MoveNext()
		{
			this.SelectedFile = GetNext();
		}

		private PngFileViewModel GetNext()
		{
			var sourceList = this.IsGrouped ? this.promptGroups[this.SelectedFile.FullPromptHash] : filteredFiles;
			return sourceList.GetNext(this.SelectedFile);
		}

		public IList<GalleryRow> Rows { get; private set; }

		public void ToggleExpandedState(PngFileViewModel model)
		{
			this.ExpandedFile = model == this.ExpandedFile ? null : model;
			RunChunking();
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
