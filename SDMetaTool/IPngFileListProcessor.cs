using System.Collections.Generic;

namespace SDMetaTool
{
    public interface IPngFileListProcessor
    {
        void ProcessPngFiles(IEnumerable<PngFile> tracks, string root);
    }
}
