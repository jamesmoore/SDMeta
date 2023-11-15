using System.Collections;

namespace SDMetaUI.Models
{
	public class GalleryRow(IEnumerable<PngFileViewModel> list, bool isSubGroup = false, bool isStartOfGroup = false, bool isEndOfGroup = false) : IEnumerable<PngFileViewModel>
	{
		public bool IsSubGroup { get; private set; } = isSubGroup;
		public bool IsStartOfGroup { get; private set; } = isStartOfGroup;
		public bool IsEndOfGroup { get; private set; } = isEndOfGroup;

		public IEnumerator<PngFileViewModel> GetEnumerator()
		{
			return list.GetEnumerator() ;
		}

		internal void Remove(PngFileViewModel selectedFile)
		{
			list = list.Where(p => p != selectedFile).ToList();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return list.GetEnumerator();
		}
	}
}
