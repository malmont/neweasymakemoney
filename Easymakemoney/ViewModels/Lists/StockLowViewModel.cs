namespace Easymakemoney.ViewModels.Lists
{
    /// <summary>
    /// Liste d'articles : ouverte par la vignette « stock bas » (paramètre Tile) ou par un conseil du guide
    /// (paramètres Items, Title, Intro), sans nouvel appel au backend.
    /// </summary>
    public partial class StockLowViewModel : BaseViewModel, IQueryAttributable
    {
        public ObservableCollection<LowStockItem> Items { get; } = new();

        [ObservableProperty]
        private string _pageTitle = "Stock bas";

        [ObservableProperty]
        private string? _intro;

        [ObservableProperty]
        private string? _sourcesText;

        [ObservableProperty]
        private bool _isEmpty;

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            IEnumerable<LowStockItem> items;
            if (query.TryGetValue("Tile", out var value) && value is HomeTile tile)
            {
                PageTitle = "Stock bas";
                Intro = tile.text;
                SourcesText = tile.SourcesText;
                items = tile.LowStockItems();
            }
            else if (query.TryGetValue("Items", out var list) && list is IEnumerable<LowStockItem> adviceItems)
            {
                PageTitle = query.TryGetValue("Title", out var title) && title is string text ? text : "Articles";
                Intro = query.TryGetValue("Intro", out var intro) ? intro as string : null;
                SourcesText = null;
                items = adviceItems;
            }
            else
            {
                return;
            }

            Items.Clear();
            foreach (var item in items)
                Items.Add(item);
            IsEmpty = Items.Count == 0;
        }
    }
}
