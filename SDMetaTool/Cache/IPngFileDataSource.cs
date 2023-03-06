using System;
using System.Collections.Generic;

namespace SDMetaTool.Cache
{
    public interface IPngFileDataSource : IDisposable
    {
        IEnumerable<PngFileSummary> Query(string filter);
		IEnumerable<string> GetAllFilenames();

		PngFile ReadPngFile(string realFileName);
        void WritePngFile(PngFile info);
		void BeginTransaction();
		void CommitTransaction();
        IEnumerable<ModelSummary> GetModelSummaryList();
	}
}