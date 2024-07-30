namespace Easymakemoney.ViewModels.Lists
{
    public partial class ListNewCollectionViewModel : BaseViewModel
    {
        private readonly GetListCollectionsUseCase _getListCollectionsUseCase;
        public required ObservableCollection<ListCollection> _listCollections = new ObservableCollection<ListCollection>();

        public ObservableCollection<ListCollection> ListCollections
        {
            get => _listCollections;
            set { _listCollections = value; OnPropertyChanged(nameof(ListCollections)); }
        }

        // Constructeur
        public ListNewCollectionViewModel(GetListCollectionsUseCase getListCollectionsUseCase)
        {
            _getListCollectionsUseCase = getListCollectionsUseCase;
        }

        // Commandes
        public async Task GetListCollectionCommand()
        {
            ListCollections = await _getListCollectionsUseCase.ExecuteAsync();
        }

        [ICommand]
        public async Task GetRandomListCollectionAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                var listCollections = await _getListCollectionsUseCase.ExecuteAsync();
                ListCollections.Clear();

                foreach (var listCollection in listCollections)
                    ListCollections.Add(listCollection);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await Shell.Current.DisplayAlert("Error", "Unable to get products", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
