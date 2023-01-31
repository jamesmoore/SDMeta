using System.Collections.Generic;

namespace SDMetaTool
{
    public interface IFileLister
    {
        IEnumerable<string> GetList(string path);
    }
}