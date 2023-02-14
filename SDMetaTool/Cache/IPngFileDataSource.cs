using System;
using System.Collections.Generic;

namespace SDMetaTool.Cache
{
    public interface IPngFileDataSource : IDisposable
    {
        IEnumerable<PngFile> GetAll();
        PngFile ReadPngFile(string realFileName);
        void WritePngFile(PngFile info);
		void BeginTransaction();
		void CommitTransaction();
        IEnumerable<ModelSummary> GetModelSummaryList();
	}
}