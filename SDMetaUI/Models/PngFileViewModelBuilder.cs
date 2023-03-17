using SDMetaTool;
using System.IO.Abstractions;

namespace SDMetaUI.Models
{
	public class PngFileViewModelBuilder
	{
		private readonly IFileSystem fileSystem;

		public PngFileViewModelBuilder(IFileSystem fileSystem)
		{
			this.fileSystem = fileSystem;
		}

		public PngFileViewModel BuildModel(PngFileSummary p)
		{
			return new PngFileViewModel(p.FileName, p.FullPromptHash, fileSystem.Path.GetFileName);
		}
	}
}
