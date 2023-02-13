using System.Collections.Generic;
using System.Threading.Tasks;

namespace SDMetaTool
{
    public interface IPngFileListProcessor
    {
        Task ProcessPngFiles(string root);
    }
}
