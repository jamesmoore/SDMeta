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
			return new PngFileViewModel(fileSystem.Path.GetFileName)
			{
				FileName = p.FileName,
				FullPromptHash = p.FullPromptHash,
			};
		}
	}
}
