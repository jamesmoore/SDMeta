using System.Collections;

namespace SDMetaUI.Models
{
	public class GalleryRow(IEnumerable<ImageFileViewModel> list, bool isSubGroup = false, bool isStartOfGroup = false, bool isEndOfGroup = false) : IEnumerable<ImageFileViewModel>
	{
		public bool IsSubGroup { get; private set; } = isSubGroup;
		public bool IsStartOfGroup { get; private set; } = isStartOfGroup;
		public bool IsEndOfGroup { get; private set; } = isEndOfGroup;

		public IEnumerator<ImageFileViewModel> GetEnumerator()
		{
			return list.GetEnumerator() ;
		}

		internal void Remove(ImageFileViewModel selectedFile)
		{
			list = list.Where(p => p != selectedFile).ToList();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return list.GetEnumerator();
		}
	}
}
