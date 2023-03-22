namespace SDMetaUI.Models
{
	public class GroupedByPromptList : IGroupList
	{
		private IList<PngFileViewModel>? groupedFiles = null;
		private IDictionary<string, List<PngFileViewModel>>? promptGroups = null;
		private readonly FilteredList FilteredList;
		public PngFileViewModel? ExpandedFile { get; set; }

		public GroupedByPromptList(FilteredList filteredList)
		{
			FilteredList = filteredList;
		}

		public void RunGrouping()
		{
			if (this.ExpandedFile != null)
			{
				this.ExpandedFile = this.FilteredList.Get(ExpandedFile.FileName);
			}
			
			this.promptGroups = this.FilteredList.FilteredFiles.GroupBy(p => p.FullPromptHash).ToDictionary(p => p.Key, p => p.ToList());

			groupedFiles = this.promptGroups.Select(p => p.Value.First()).ToList();
			foreach (var file in groupedFiles)
			{
				file.SubItems = this.promptGroups[file.FullPromptHash];
			}
		}

		public IList<GalleryRow> GetChunks(int countPerRow)
		{
			if ((this.ExpandedFile?.SubItems?.Any() ?? false))
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
				return before.Concat(middle).Concat(after).ToList();
			}
			else
			{
				return groupedFiles.Chunk(countPerRow).Select(p => new GalleryRow(p)).ToList();
			}
		}

		public PngFileViewModel GetPrevious(PngFileViewModel current)
		{
			return this.promptGroups[current.FullPromptHash].GetPrevious(current);
		}

		public PngFileViewModel GetNext(PngFileViewModel current)
		{
			return this.promptGroups[current.FullPromptHash].GetNext(current);
		}
		public void Remove(PngFileViewModel current)
		{
			this.groupedFiles.Remove(current);
		}
		public void Replace(PngFileViewModel current, PngFileViewModel replacement)
		{
			this.groupedFiles.Replace(current, replacement);
		}
	}
}
