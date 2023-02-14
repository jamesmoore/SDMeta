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
			var parameters = pngFileViewModel.Parameters;
			if (parameters == null && string.IsNullOrWhiteSpace(this.Model) && string.IsNullOrWhiteSpace(ModelHash)) return true;
			if (parameters != null && parameters.Model == this.Model && parameters.ModelHash == this.ModelHash) return true;
			return false;
		}

	}
}
