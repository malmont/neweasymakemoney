// Code antérieur aux annotations de nullité : avertissements coupés ici, sans effet sur l'exécution.
#nullable disable warnings
public partial class BottomSheetPopupListViewViewModel : ObservableObject
{
    private Popup _popup;
    private readonly Func<Task<IEnumerable<IListItem>>> _getItemsFunc;
    private readonly Action<IListItem> _onItemTappedAction;

    public ObservableCollection<IListItem> Items { get; set; } = new ObservableCollection<IListItem>();

    public BottomSheetPopupListViewViewModel(Func<Task<IEnumerable<IListItem>>> getItemsFunc, Action<IListItem> onItemTappedAction)
    {
        _getItemsFunc = getItemsFunc;
        _onItemTappedAction = onItemTappedAction;
        ItemTappedCommand = new AsyncRelayCommand<IListItem>(OnItemTapped);
    }

    public ICommand ItemTappedCommand { get; }

    public async Task GetItemsAsync()
    {
        Items.Clear();
        try
        {
            var items = await _getItemsFunc();

            foreach (var item in items)
            {
                Items.Add(item);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            await Easymakemoney.Utilities.AppDialogs.InfoAsync("Erreur", "Impossible de charger la liste. Réessayez.");
        }
    }

    private async Task OnItemTapped(IListItem? item)
    {
        if (item is null)
            return;
        _onItemTappedAction(item);
        await CancelAsync();
    }

    public void SetPopupInstance(Popup popup)
    {
        _popup = popup;
    }

    private Task CancelAsync() => _popup.CloseAsync();
}
