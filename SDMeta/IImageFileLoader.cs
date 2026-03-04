using System.Threading.Tasks;

namespace SDMeta
{
	public interface IImageFileLoader
	{
		Task<ImageFile> GetPngFile(string filename);
	}
}