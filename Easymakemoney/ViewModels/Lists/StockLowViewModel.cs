namespace Easymakemoney.ViewModels.Lists
{
    /// <summary>Écran « Stock bas » ouvert par la vignette d'alerte : les articles qui se vendent et vont manquer.</summary>
    public partial class StockLowViewModel : BaseViewModel, IQueryAttributable
    {
        public ObservableCollection<LowStockItem> Items { get; } = new();

        [ObservableProperty]
        private string? _intro;

        [ObservableProperty]
        private string? _sourcesText;

        [ObservableProperty]
        private bool _isEmpty;

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (!query.TryGetValue("Tile", out var value) || value is not HomeTile tile)
                return;

            Intro = tile.text;
            SourcesText = tile.SourcesText;
            Items.Clear();
            foreach (var item in tile.LowStockItems())
                Items.Add(item);
            IsEmpty = Items.Count == 0;
        }
    }
}
