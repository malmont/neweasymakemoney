
using Easymakemoney.Services.Dialog;

namespace Easymakemoney.ViewModels.Lists
{
    public partial class ListNewCollectionViewModel : BaseViewModel
    {
        private readonly GetListCollectionsUseCase _getListCollectionsUseCase;
        private readonly DeleteCollectionUseCase _deleteCollectionUseCase;
        private readonly CollectionFormModel _collectionFormModel;
        private readonly SaveCollectionUseCase _saveCollectionUseCase;
        private readonly IDialogService _dialogService;

        public ObservableCollection<ListCollection> ListCollections { get; set; } = new ObservableCollection<ListCollection>();

        public ListNewCollectionViewModel(GetListCollectionsUseCase getListCollectionsUseCase,SaveCollectionUseCase saveCollectionUseCase,
        DeleteCollectionUseCase deleteCollectionUseCase,
        CollectionFormModel collectionFormModel,
        IDialogService dialogService)
        {
            _saveCollectionUseCase = saveCollectionUseCase;
            _getListCollectionsUseCase = getListCollectionsUseCase;
            _deleteCollectionUseCase = deleteCollectionUseCase;
            _dialogService = dialogService;
            ItemTappedCommand = new AsyncRelayCommand<ListCollection>(OnItemTapped);
            DeleteCollectionCommand = new AsyncRelayCommand<ListCollection>(DeleteCollection);
            _collectionFormModel = collectionFormModel;
        }
        public ICommand ItemTappedCommand { get; }
        public ICommand DeleteCollectionCommand { get; }

        [RelayCommand]
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
                await _dialogService.ShowErrorAsync(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task ShowBottomSheetAsync()
        {
            try
            {
                var viewModel = new BottomSheetPopupViewModel(
                    popup: new Popup(),
                    saveCollectionUseCase: _saveCollectionUseCase,
                    isCollectionForm: true,
                    collectionViewModel: this,
                    collectionFormModel: _collectionFormModel,
                    dialogService: _dialogService);

                var popup = new BottomSheetPopup(viewModel);
                var mainPage = Application.Current?.MainPage;
                if (mainPage != null)
                {
                    Console.WriteLine("AShowPopupAsync");
                    await mainPage.ShowPopupAsync(popup);
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync(ex);
                Console.WriteLine($"Exception: {ex}");
            }
        }

        private async Task OnItemTapped(ListCollection? collection)
        {
            if (collection == null)
                return;

            // Navigate to the ListNewCommandPage and pass the collection ID
            if (Shell.Current is { } shell)
                await shell.GoToAsync($"{nameof(ListNewCommandPage)}?CollectionId={collection.id}&Closed={collection.IsClosed}", true);
        }

        private async Task DeleteCollection(ListCollection? collection)
        {
            if (collection == null)
                return;

            bool confirmed = await _dialogService.ConfirmAsync(
                "Confirmation",
                "Êtes-vous sûr de vouloir supprimer cette collection ?",
                "Supprimer",
                "Annuler");
            if (!confirmed)
                return;

            try
            {
                var result = await _deleteCollectionUseCase.ExecuteAsync(collection.id);
                if (result)
                {
                    await GetListCollectionAsync();
                    await _dialogService.ShowInfoAsync("Collection supprimée avec succès.");
                }
                else
                {
                    await _dialogService.ShowInfoAsync("Échec de la suppression de la collection.");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync(ex);
            }
        }
    }
}
