using System.Collections.Generic;

namespace SDMetaTool.Cache
{
    public interface IPngFileDataSource
    {
        IEnumerable<PngFile> GetAll();
        PngFile ReadPngFile(string realFileName);
        void WritePngFile(PngFile info);
        void ClearAll();
	}
}