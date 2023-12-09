using System.Collections.Generic;

namespace SDMeta
{
	public interface IImageDir
	{
		IEnumerable<string> GetPath();
	}
}