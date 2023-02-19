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
				Expandedfiles = null;
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
					this.Expandedfiles = null;
				}
				RunGrouping();
			}
		}

		private void RunGrouping()
		{
			groupedFiles = isGrouped ?
				filteredFiles.GroupBy(p => p.FullPromptHash).Select(p => p.LastOrDefault()).ToList() :
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
				int countPerRow = (width - 17) / (ThumbnailService.ThumbnailSize + 8 * 2);

				if (this.IsGrouped && (this.Expandedfiles?.Any() ?? false))
				{
					var position = this.groupedFiles.TakeWhile(p => p != this.ExpandedFile).Count() + 1;
					var modulo = position % countPerRow;
					if (modulo > 0)
					{
						position += (countPerRow - modulo);
					}
					var before = this.groupedFiles.Take(position).Chunk(countPerRow).Select(p => new GalleryRow(p));
					var expandedChunks = this.Expandedfiles.Chunk(countPerRow).ToList();
					var middle = expandedChunks.Select((p, i) => new GalleryRow(p, true, i == 0, i == expandedChunks.Count - 1));
					var after = this.groupedFiles.Skip(position).Chunk(countPerRow).Select(p => new GalleryRow(p));
					this.ChunkedFiles = before.Concat(middle).Concat(after).ToList();
				}
				else
				{
					this.ChunkedFiles = groupedFiles.Chunk(countPerRow).Select(p => new GalleryRow(p)).ToList();
				}
			}
		}

		public void RemoveFile()
		{
			if (this.SelectedFile != null)
			{
				allFiles.Remove(this.SelectedFile);
				filteredFiles.Remove(this.SelectedFile);
				groupedFiles.Remove(this.SelectedFile);
				this.Expandedfiles?.Remove(this.SelectedFile);
				foreach (var row in this.ChunkedFiles.Where(p => p.Contains(this.SelectedFile)))
				{
					row.Remove(this.SelectedFile);
				}
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

		public IList<GalleryRow> ChunkedFiles { get; private set; }

		private IList<PngFileViewModel> Expandedfiles { get; set; }

		private IList<PngFileViewModel> GetFilesMatchingPromptHash(string hash) => filteredFiles.Where(p => p.FullPromptHash == hash).ToList();

		public void ToggleExpandedState(PngFileViewModel model)
		{
			this.ExpandedFile = model == this.ExpandedFile ? null : model;

			if (this.ExpandedFile != null)
			{
				this.Expandedfiles = this.GetFilesMatchingPromptHash(model.FullPromptHash);
			}
			else
			{
				this.Expandedfiles = null;
			}
			RunChunking();
		}
	}
}
