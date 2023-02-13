using System.Threading.Tasks;

namespace SDMetaTool
{
    public interface IPngFileLoader
    {
        Task<PngFile> GetPngFile(string filename);
    }
}