namespace SDMetaUI.Models
{
	public class GalleryViewModel
	{
		private IList<PngFileViewModel> allFiles = null;
		private IList<PngFileViewModel> filteredFiles = null;
		private IList<PngFileViewModel> groupedFiles = null;

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
				filteredFiles = allFiles.ToList();
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
				RunGrouping();
				if (isGrouped == false)
				{
					this.Expandedfiles = Enumerable.Empty<PngFileViewModel>().ToList();
				}
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
				int countPerRow = (width - 17) / 191;
				this.ChunkedFiles = groupedFiles.Chunk(countPerRow).Select(p => p.ToList()).ToList();
			}
		}

		public void RemoveFile(PngFileViewModel selectedFile)
		{
			allFiles.Remove(selectedFile);
			filteredFiles.Remove(selectedFile);
			groupedFiles.Remove(selectedFile);
			this.Expandedfiles?.Remove(selectedFile);
			foreach (var row in this.ChunkedFiles.Where(p => p.Contains(selectedFile)))
			{
				row.Remove(selectedFile);
			}
		}

		public PngFileViewModel GetPrevious(PngFileViewModel selectedFile)
		{
			var index = filteredFiles.IndexOf(selectedFile);
			return index > 0 ? filteredFiles[index - 1] : selectedFile;
		}

		public PngFileViewModel GetNext(PngFileViewModel selectedFile)
		{
			var index = filteredFiles.IndexOf(selectedFile);
			return index < filteredFiles.Count - 1 ? filteredFiles[index + 1] : selectedFile;
		}

		public IList<List<PngFileViewModel>> ChunkedFiles { get; private set; }

		public IList<PngFileViewModel> Expandedfiles { get; set; }

		private IList<PngFileViewModel> GetFilesMatchingPromptHash(string hash) => filteredFiles.Where(p => p.FullPromptHash == hash).ToList();

		public void ToggleExpandedState(PngFileViewModel model)
		{
			model.Expanded = !model.Expanded;
			foreach (var file in filteredFiles.Where(p => p.Expanded && p != model))
			{
				file.Expanded = false;
			}

			if (model.Expanded)
			{
				this.Expandedfiles = this.GetFilesMatchingPromptHash(model.FullPromptHash);
			}
			else
			{
				this.Expandedfiles = Enumerable.Empty<PngFileViewModel>().ToList();
			}
		}
	}
}
