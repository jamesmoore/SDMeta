using SDMetaUI.Services;

namespace SDMetaUI.Models
{
	public class GalleryViewModel
	{
		private IList<PngFileViewModel> allFiles = null;
		private IList<PngFileViewModel> filteredFiles = null;
		private IList<PngFileViewModel> groupedFiles = null;
		public PngFileViewModel SelectedFile { get; set; }
		public PngFileViewModel ExpandedFile { get; private set; }
		private ModelSummaryViewModel modelFilter;
		public ModelSummaryViewModel ModelFilter
		{
			get
			{
				return modelFilter;
			}
			set
			{
				modelFilter = value;
				RunFilter();
			}
		}

		public void Initialize(IList<PngFileViewModel> all)
		{
			allFiles = all;
			RunFilter();
		}

		public bool HasData => allFiles != null;

		public int AllFileCount => allFiles.Count;

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
				filter = value;
				RunFilter();
			}
		}

		private void RunFilter()
		{
			if (string.IsNullOrWhiteSpace(filter) == false)
			{
				filteredFiles = allFiles.Where(p =>
								p.Prompt.Contains(filter, StringComparison.InvariantCultureIgnoreCase) ||
								p.FileName.Contains(filter, StringComparison.InvariantCultureIgnoreCase) ||
								p.Parameters?.Seed.ToString() == filter
							).ToList();
			}
			else
			{
				filteredFiles = allFiles;
			}

			if (this.ModelFilter != null)
			{
				filteredFiles = filteredFiles.Where(p => this.ModelFilter.Matches(p)).ToList();
			}

			if (filteredFiles.Contains(ExpandedFile) == false)
			{
				ExpandedFile = null;
			}
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
				isGrouped = value;
				if (isGrouped == false)
				{
					this.ExpandedFile = null;
				}
				RunGrouping();
			}
		}

		private void RunGrouping()
		{
			groupedFiles = isGrouped ?
				filteredFiles.Where(p => p.SubItems != null).ToList() :
				filteredFiles;
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
				allFiles.Remove(this.SelectedFile);
				filteredFiles.Remove(this.SelectedFile);
				groupedFiles.Remove(this.SelectedFile);
				this.ExpandedFile?.SubItems?.Remove(this.SelectedFile);
				foreach (var row in this.Rows.Where(p => p.Contains(this.SelectedFile)))
				{
					row.Remove(this.SelectedFile);
				}
				RunChunking();
				this.SelectedFile = null;
			}
		}

		public void GetPrevious()
		{
			var index = filteredFiles.IndexOf(this.SelectedFile);
			this.SelectedFile = index > 0 ? filteredFiles[index - 1] : this.SelectedFile;
		}

		public void GetNext()
		{
			var index = filteredFiles.IndexOf(this.SelectedFile);
			this.SelectedFile = index < filteredFiles.Count - 1 ? filteredFiles[index + 1] : this.SelectedFile;
		}

		public IList<GalleryRow> Rows { get; private set; }

		public void ToggleExpandedState(PngFileViewModel model)
		{
			this.ExpandedFile = model == this.ExpandedFile ? null : model;
			RunChunking();
		}
	}
}
