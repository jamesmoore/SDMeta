using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SDMetaTool.Cache
{
    public interface IPngFileDataSource : IAsyncDisposable
    {
        Task<IEnumerable<PngFile>> GetAll();
        Task<PngFile> ReadPngFile(string realFileName);
        Task WritePngFile(PngFile info);
		Task BeginTransaction();
		Task CommitTransaction();
	}
}