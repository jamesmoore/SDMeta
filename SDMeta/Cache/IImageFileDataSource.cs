using System;
using System.Collections.Generic;

namespace SDMeta.Cache
{
	public interface IImageFileDataSource : IDisposable
	{
		IEnumerable<ImageFileSummary> Query(QueryParams queryParams);
		IEnumerable<string> GetAllFilenames();

		ImageFile? ReadImageFile(string realFileName);
		void WriteImageFile(ImageFile info);
		void BeginTransaction();
		void CommitTransaction();
		IEnumerable<ModelSummary> GetModelSummaryList();
		void Truncate();
		void PostUpdateProcessing();
        void Initialize();
    }

	public record QueryParams(string? Filter, ModelFilter? ModelFilter, QuerySortBy QuerySortBy);

	public record class ModelFilter(string? Model, string? ModelHash);

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