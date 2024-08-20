namespace Easymakemoney.ViewModels.Components
{
    public partial class BottomSheetPopupListViewViewModel : ObservableObject
    {
        private Popup _popup;

        private readonly GetListCollectionsUseCase _getListCollectionsUseCase;

        private readonly ListNewNoteDeFraisViewModel _noteDeFraisViewModel;

        public ObservableCollection<ListCollection> ListCollections { get; set; } = new ObservableCollection<ListCollection>();

        public BottomSheetPopupListViewViewModel(Popup popup = null,GetListCollectionsUseCase getListCollectionsUseCase=null, ListNewNoteDeFraisViewModel noteDeFraisViewModel=null)
        {
            _noteDeFraisViewModel = noteDeFraisViewModel;
            _popup = popup;
            _getListCollectionsUseCase = getListCollectionsUseCase;
            // _noteDeFraisViewModel.CollectionId = CollectionId;
            ItemTappedCommand = new RelayCommand<ListCollection>(OnItemTapped);

        }
        public ICommand ItemTappedCommand { get; }

        public int CollectionId { get; set; }

          [ICommand]
        public async Task GetListCollectionAsync()
        {
         
            ListCollections.Clear();
            try
            {
                var collections = await _getListCollectionsUseCase.ExecuteAsync();

                foreach (var collection in collections)
                {
                    ListCollections.Add(collection);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await Shell.Current.DisplayAlert("Error", "Unable to get collections", "OK");
            }
        
        }
       private async void OnItemTapped(ListCollection collection)
        {
            if (collection == null)
                return;

            _noteDeFraisViewModel.CollectionId = collection.id;
           await _noteDeFraisViewModel.GetListNoteDeFraisAsync();
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
}