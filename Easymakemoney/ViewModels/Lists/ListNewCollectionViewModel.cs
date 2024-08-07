
using System.Windows.Input;

using Easymakemoney.Components;

namespace Easymakemoney.ViewModels.Lists
{
    public partial class ListNewCollectionViewModel : BaseViewModel
    {
        private readonly GetListCollectionsUseCase _getListCollectionsUseCase;
        private readonly CreateCollectionUseCase _createCollectionUseCase;
        private readonly DeleteCollectionUseCase _deleteCollectionUseCase;
        private readonly IPreferenceService _preferenceService;

        public ObservableCollection<ListCollection> ListCollections { get; set; } = new ObservableCollection<ListCollection>();

        public ListNewCollectionViewModel(GetListCollectionsUseCase getListCollectionsUseCase, CreateCollectionUseCase createCollectionUseCase, IPreferenceService preferenceService,DeleteCollectionUseCase deleteCollectionUseCase)
        {
            _getListCollectionsUseCase = getListCollectionsUseCase;
            _createCollectionUseCase = createCollectionUseCase;
            _preferenceService = preferenceService;
            _deleteCollectionUseCase = deleteCollectionUseCase;

            DeleteCollectionCommand = new RelayCommand<ListCollection>(DeleteCollection!);
        }

        public ICommand DeleteCollectionCommand { get; }

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
            var mainPage = Application.Current?.MainPage;
            if (mainPage != null)
            {
                var popup = new BottomSheetPopup(_createCollectionUseCase, _preferenceService, this);
                mainPage.ShowPopup(popup);
            }
        }

        private async void DeleteCollection(ListCollection collection)
        {
            // if (ListCollections.Contains(collection))
            // {
            //     ListCollections.Remove(collection);
            //     // Ajouter une logique pour supprimer la collection sur le serveur si nécessaire
            // }
             var result = await _deleteCollectionUseCase.ExecuteAsync(collection.id);
                if (result)
                {
                    GetListCollectionAsync();
                    await Shell.Current.DisplayAlert("Success", "Collection deleted", "OK");
                }
                else
                {
                    await Shell.Current.DisplayAlert("Error", "Failed to delete collection", "OK");
                }

        }
    }
}
