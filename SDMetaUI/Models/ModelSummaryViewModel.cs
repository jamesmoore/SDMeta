using SDMeta;

namespace SDMetaUI.Models
{
    public class ModelSummaryViewModel
    {
        public static readonly ModelSummaryViewModel AllModels = new(0, "-- all models --");

        private ModelSummaryViewModel(int id, string text)
        {
            Id = id;
            Text = text;
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
