using SDMeta;
using System.IO.Abstractions;

namespace SDMetaUI.Models
{
	public class ImageFileViewModelBuilder(IFileSystem fileSystem)
	{
		public ImageFileViewModel BuildModel(ImageFileSummary p)
		{
			return new ImageFileViewModel(p.FileName, p.FullPromptHash, fileSystem.Path.GetFileName);
		}
	}
}
