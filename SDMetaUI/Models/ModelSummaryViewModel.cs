using SDMetaTool;

namespace SDMetaUI.Models
{
	public class ModelSummaryViewModel
	{
		public ModelSummaryViewModel() {
			Id = 0; 
			Text = "-- all models --";
		}

		public ModelSummaryViewModel(ModelSummary p, int id)
		{
			Id = id;
			Count = p.Count;
			Model = p.Model;
			ModelHash = p.ModelHash;
			Text = (p.ModelHash ?? "<empty>") + " (" + (p.Model ?? "<no name>") + ") [" + p.Count + "]";
		}

		public int Id { get; }
		public string? Model { get; }
		public string? ModelHash { get; }
		public int Count { get; }
		public string Text { get; }
	}
}
