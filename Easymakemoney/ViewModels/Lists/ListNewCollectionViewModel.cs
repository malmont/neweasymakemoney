using Easymakemoney.Components;

namespace Easymakemoney.ViewModels.Lists
{
    public partial class ListNewCollectionViewModel : BaseViewModel
    {
        private readonly GetListCollectionsUseCase _getListCollectionsUseCase;
        private readonly CreateCollectionUseCase _createCollectionUseCase;
        private readonly IPreferenceService _preferenceService;

        public ObservableCollection<ListCollection> ListCollections { get; set; } = new ObservableCollection<ListCollection>();

        public ListNewCollectionViewModel(GetListCollectionsUseCase getListCollectionsUseCase, CreateCollectionUseCase createCollectionUseCase, IPreferenceService preferenceService)
        {
            _getListCollectionsUseCase = getListCollectionsUseCase;
            _createCollectionUseCase = createCollectionUseCase;
            _preferenceService = preferenceService;
        }

        [ICommand]
        public async Task GetListCollectionAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;
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
            finally
            {
                IsBusy = false;
            }
        }

        [ICommand]
        public void ShowBottomSheet()
        {
            var popup = new BottomSheetPopup(_createCollectionUseCase, _preferenceService, this);
            var mainPage = Application.Current?.MainPage;

            if (mainPage != null)
            {
                mainPage.ShowPopup(popup);
            }
        }
    }
}
