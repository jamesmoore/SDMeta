using SDMetaTool.Cache;
using System.Collections;

namespace SDMetaUI.Models
{
	public class FilteredList : IList<PngFileViewModel>
	{
		public FilteredList(
			IPngFileDataSource pngFileDataSource,
			PngFileViewModelBuilder pngFileViewModelBuilder,
			Action postFilteringAction
			)
		{
			this.pngFileDataSource = pngFileDataSource;
			this.pngFileViewModelBuilder = pngFileViewModelBuilder;
			this.postFilteringAction = postFilteringAction;
			this.filteredFiles = new List<PngFileViewModel>();
		}
		private readonly Action postFilteringAction;
		private readonly IPngFileDataSource pngFileDataSource;
		private readonly PngFileViewModelBuilder pngFileViewModelBuilder;
		private IList<PngFileViewModel> filteredFiles;

		public bool FilterError { get; private set; }

		private ModelSummaryViewModel modelFilter;
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

		public int Count => this.filteredFiles.Count;

		public bool IsReadOnly => throw new NotImplementedException();

		public PngFileViewModel this[int index]
		{
			get => this.filteredFiles[index];
			set => throw new NotImplementedException();
		}

		public void RunFilter()
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
			try
			{
				filteredFiles = pngFileDataSource.Query(queryParams).OrderByDescending(p => p.LastUpdated).Select(p => pngFileViewModelBuilder.BuildModel(p)).ToList();
				this.FilterError = false;
			}
			catch (Exception ex)
			{
				if(ex.Message.Contains("fts5"))
				{
					this.FilterError = true;
				}
				else
				{
					throw;
				}
			}
			postFilteringAction();
		}

		public PngFileViewModel? Get(string path)
		{
			return filteredFiles.FirstOrDefault(p => p.FileName == path);
		}

		public int IndexOf(PngFileViewModel item)
		{
			return filteredFiles.IndexOf(item);
		}

		public void Insert(int index, PngFileViewModel item)
		{
			throw new NotImplementedException();
		}

		public void RemoveAt(int index)
		{
			throw new NotImplementedException();
		}

		public void Add(PngFileViewModel item)
		{
			throw new NotImplementedException();
		}

		public void Clear()
		{
			throw new NotImplementedException();
		}

		public bool Contains(PngFileViewModel item)
		{
			throw new NotImplementedException();
		}

		public void CopyTo(PngFileViewModel[] array, int arrayIndex)
		{
			throw new NotImplementedException();
		}

		public bool Remove(PngFileViewModel item)
		{
			return this.filteredFiles.Remove(item);
		}

		public IEnumerator<PngFileViewModel> GetEnumerator()
		{
			return this.filteredFiles.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.filteredFiles.GetEnumerator();
		}
	}
}
