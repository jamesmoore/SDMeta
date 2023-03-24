namespace SDMetaUI.Models
{
	public class GroupedByPromptList : IGroupList, IExpandable
	{
		private IList<PngFileViewModel>? groupedFiles = null;
		private IDictionary<string, List<PngFileViewModel>>? promptGroups = null;
		private readonly FilteredList filteredList;
		private readonly Action postGroupingAction;

		public PngFileViewModel? ExpandedFile { get; private set; }

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

		public GroupedByPromptList(
			FilteredList filteredList,
			Action postGroupingAction)
		{
			this.filteredList = filteredList;
			this.postGroupingAction = postGroupingAction;
			this.RunGrouping();
		}

		public void RunGrouping()
		{
			if (this.ExpandedFile != null)
			{
				this.ExpandedFile = this.filteredList.Get(ExpandedFile.FileName);
			}

			this.promptGroups = this.filteredList.GroupBy(p => p.FullPromptHash).ToDictionary(p => p.Key, p => p.ToList());

			groupedFiles = this.promptGroups.Select(p => p.Value.First()).ToList();
			foreach (var file in groupedFiles)
			{
				file.SubItems = this.promptGroups[file.FullPromptHash];
			}
			postGroupingAction();
		}

		public IList<GalleryRow> GetChunks()
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

		public PngFileViewModel GetPrevious(PngFileViewModel current) => this.promptGroups[current.FullPromptHash].GetPrevious(current);

		public PngFileViewModel GetNext(PngFileViewModel current) => this.promptGroups[current.FullPromptHash].GetNext(current);

		public void Remove(PngFileViewModel current)
		{
			if (current == this.ExpandedFile && this.ExpandedFile.SubItems?.Count > 1)
			{
				this.ExpandedFile.SubItems.Remove(current);
				var replacement = this.ExpandedFile.SubItems.First();
				replacement.SubItems = this.ExpandedFile.SubItems;
				this.groupedFiles.Replace(current, replacement);
				this.ExpandedFile = replacement;
			}
			else
			{
				this.ExpandedFile?.SubItems?.Remove(current);
				this.groupedFiles.Remove(current);
			}
			postGroupingAction();
		}

		public void ToggleExpandedState(PngFileViewModel model)
		{
			this.ExpandedFile = model == this.ExpandedFile ? null : model;
			postGroupingAction();
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
