﻿using System;
using System.Collections.Generic;

namespace SDMetaTool.Cache
{
    public interface IPngFileDataSource : IDisposable
    {
        IEnumerable<PngFileSummary> Query(QueryParams queryParams);
		IEnumerable<string> GetAllFilenames();

		PngFile ReadPngFile(string realFileName);
        void WritePngFile(PngFile info);
		void BeginTransaction();
		void CommitTransaction();
        IEnumerable<ModelSummary> GetModelSummaryList();
	}

	public class QueryParams
	{
		public string Filter { get; set; }
		public ModelFilter ModelFilter { get; set; }
	}

	public class ModelFilter
	{
		public string Model { get; set; }
		public string ModelHash { get; set; }
	}
}