using SDMeta;
using System.IO.Abstractions;

namespace SDMetaUI.Models
{
	public class PngFileViewModelBuilder(IFileSystem fileSystem)
	{
		public PngFileViewModel BuildModel(PngFileSummary p)
		{
			return new PngFileViewModel(p.FileName, p.FullPromptHash, fileSystem.Path.GetFileName);
		}
	}
}
