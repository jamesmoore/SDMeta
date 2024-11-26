using System.Threading.Tasks;

namespace SDMeta
{
	public interface IPngFileLoader
	{
		Task<PngFile> GetPngFile(string filename);
	}
}