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
        ItemTappedCommand = new RelayCommand<IListItem>(OnItemTapped);
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
            await Shell.Current.DisplayAlert("Error", "Unable to get items", "OK");
        }
    }

    private void OnItemTapped(IListItem item)
    {
        _onItemTappedAction(item);
        Cancel();
    }

    public void SetPopupInstance(Popup popup)
    {
        _popup = popup;
    }

    private void Cancel()
    {
        _popup.Close();
    }
}
