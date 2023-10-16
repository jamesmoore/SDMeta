using System.Collections.Generic;

namespace SDMeta
{
	public interface IFileLister
	{
		IEnumerable<string> GetList(string path);
	}
}