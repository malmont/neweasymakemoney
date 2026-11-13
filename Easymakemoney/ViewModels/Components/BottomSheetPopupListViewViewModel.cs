// Code antérieur aux annotations de nullité : avertissements coupés ici, sans effet sur l'exécution.
#nullable disable warnings
public partial class BottomSheetPopupListViewViewModel : ObservableObject
{
    private Popup _popup;
    private readonly Func<Task<IEnumerable<IListItem>>> _getItemsFunc;
    private readonly Func<IListItem, Task> _onItemTapped;

    public ObservableCollection<IListItem> Items { get; set; } = new ObservableCollection<IListItem>();

    public BottomSheetPopupListViewViewModel(Func<Task<IEnumerable<IListItem>>> getItemsFunc, Action<IListItem> onItemTappedAction)
        : this(getItemsFunc, item =>
        {
            onItemTappedAction(item);
            return Task.CompletedTask;
        })
    {
    }

    /// <summary>Choix asynchrone (chargement de la période, de la commande…) : attendu, ses erreurs sont montrées au gérant.</summary>
    public BottomSheetPopupListViewViewModel(Func<Task<IEnumerable<IListItem>>> getItemsFunc, Func<IListItem, Task> onItemTappedAction)
    {
        _getItemsFunc = getItemsFunc;
        _onItemTapped = onItemTappedAction;
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
        // La fenêtre se ferme d'abord : le chargement qui suit le choix se fait sur l'écran appelant.
        await CancelAsync();
        try
        {
            await _onItemTapped(item);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            await Easymakemoney.Utilities.AppDialogs.InfoAsync("Erreur", "Ce choix n'a pas pu être chargé. Réessayez.");
        }
    }

    public void SetPopupInstance(Popup popup)
    {
        _popup = popup;
    }

    private Task CancelAsync() => _popup?.CloseAsync() ?? Task.CompletedTask;
}
