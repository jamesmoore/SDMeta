using System.IO.Abstractions;
using System.Threading.Tasks;

namespace SDMeta
{
	public interface IImageFileLoader
	{
		Task<ImageFile?> GetImageFile(IFileInfo fileInfo);
	}
}