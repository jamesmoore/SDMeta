namespace SDMetaUI
{
	public static class ExtensionMethods
	{
		public static T GetNext<T>(this IList<T> items, T item)
		{
			var index = items.IndexOf(item);
			var next = index < items.Count - 1 ? items[index + 1] : item;
			return next;
		}

		public static T GetPrevious<T>(this IList<T> items, T item)
		{
			var index = items.IndexOf(item);
			var previous = index > 0 ? items[index - 1] : item;
			return previous;
		}
	}
}
