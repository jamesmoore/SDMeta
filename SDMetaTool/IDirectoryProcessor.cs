using System.Collections.Generic;

namespace SDMetaTool
{
    public interface IDirectoryProcessor
    {
        IEnumerable<string> GetList(string path);
    }
}