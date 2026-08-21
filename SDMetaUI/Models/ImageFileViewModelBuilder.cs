using SDMeta;
using System.IO.Abstractions;

namespace SDMetaUI.Models
{
    public class ImageFileViewModelBuilder(IFileSystem fileSystem)
    {
        public ImageFileViewModel BuildModel(ImageFileSummary p)
        {
            var lastWriteTicks = fileSystem.FileInfo.New(p.FileName).LastWriteTimeUtc.Ticks.ToString();
            return new ImageFileViewModel(p.FileName, p.FullPromptHash ?? "", lastWriteTicks, fileSystem.Path.GetFileName);
        }
    }
}
