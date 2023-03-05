namespace SDMetaUI.Models
{
	public class ModelSummaryViewModel
	{
		public string Id { get; set; }
		public string Model { get; set; }
		public string ModelHash { get; set; }
		public int Count { get; set; }
		public string Text { get; set; }

		public bool Matches(PngFileViewModel pngFileViewModel)
		{
			if (Id == "0") return true;
			return pngFileViewModel.Model == this.Model && pngFileViewModel.ModelHash == this.ModelHash;
		}
	}
}
