using System.Collections;

namespace SDMetaUI.Models
{
	public class GalleryRow : IEnumerable<PngFileViewModel>
	{
		private IEnumerable<PngFileViewModel> list;

		public GalleryRow(IEnumerable<PngFileViewModel> list, bool isSubGroup = false, bool isStartOfGroup = false, bool isEndOfGroup = false)
		{
			this.list = list;
			IsSubGroup = isSubGroup;
			IsStartOfGroup = isStartOfGroup;
			IsEndOfGroup = isEndOfGroup;
		}

		public bool IsSubGroup { get; private set; }
		public bool IsStartOfGroup { get; private set; }
		public bool IsEndOfGroup { get; private set; }

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
