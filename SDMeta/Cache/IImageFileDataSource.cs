using System;
using System.Collections.Generic;

namespace SDMeta.Cache
{
	public interface IImageFileDataSource : IDisposable
	{
		IEnumerable<ImageFileSummary> Query(QueryParams queryParams);
		IEnumerable<string> GetAllFilenames();

		ImageFile ReadImageFile(string realFileName);
		void WriteImageFile(ImageFile info);
		void BeginTransaction();
		void CommitTransaction();
		IEnumerable<ModelSummary> GetModelSummaryList();
		void Truncate();
		void PostUpdateProcessing();
        void Initialize();
    }

	public class QueryParams(string filter, ModelFilter modelFilter, QuerySortBy querySort)
    {
        public string Filter { get; } = filter;
        public ModelFilter ModelFilter { get; } = modelFilter;
		public QuerySortBy QuerySortBy { get; } = querySort;
    }

	public class ModelFilter(string model, string modelHash)
    {
        public string Model { get; } = model;
        public string ModelHash { get; } = modelHash;
    }

	public enum QuerySortBy
	{
		Newest,
		Oldest,
		Largest,
		Smallest,
		AtoZ,
		ZtoA,
		Random,
	}
}