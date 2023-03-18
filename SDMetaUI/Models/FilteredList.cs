using SDMetaTool.Cache;

namespace SDMetaUI.Models
{
	public class FilteredList
	{
		public FilteredList(
			IPngFileDataSource pngFileDataSource,
			PngFileViewModelBuilder pngFileViewModelBuilder)
		{
			this.pngFileDataSource = pngFileDataSource;
			this.pngFileViewModelBuilder = pngFileViewModelBuilder;
		}

		private readonly IPngFileDataSource pngFileDataSource;
		private readonly PngFileViewModelBuilder pngFileViewModelBuilder;

		public IList<PngFileViewModel>? FilteredFiles { get; private set; }

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
			FilteredFiles = pngFileDataSource.Query(queryParams).OrderByDescending(p => p.LastUpdated).Select(p => pngFileViewModelBuilder.BuildModel(p)).ToList();
		}

		public PngFileViewModel Get(string path)
		{
			return FilteredFiles.FirstOrDefault(p => p.FileName == path);
		}
	}
}
