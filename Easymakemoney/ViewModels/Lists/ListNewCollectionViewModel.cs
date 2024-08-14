
using System.Windows.Input;

using Easymakemoney.Components;
using Easymakemoney.UseCases;
using Easymakemoney.ViewModels.FormModels;

namespace Easymakemoney.ViewModels.Lists
{
    public partial class ListNewCollectionViewModel : BaseViewModel
    {
        private readonly GetListCollectionsUseCase _getListCollectionsUseCase;
        private readonly CreateCollectionUseCase _createCollectionUseCase;
        private readonly CreateCommandUseCase _createCommandUseCase;
        private readonly DeleteCollectionUseCase _deleteCollectionUseCase;
        private readonly IPreferenceService _preferenceService;
        private readonly CollectionFormModel _collectionFormModel;
        private readonly CommandFormModel _commandFormModel;

        private readonly ProductFormModel _productFormModel;

        public ObservableCollection<ListCollection> ListCollections { get; set; } = new ObservableCollection<ListCollection>();

        public ListNewCollectionViewModel(GetListCollectionsUseCase getListCollectionsUseCase,
        CreateCollectionUseCase createCollectionUseCase,
         IPreferenceService preferenceService, DeleteCollectionUseCase deleteCollectionUseCase, CreateCommandUseCase createCommandUseCase,
        CollectionFormModel collectionFormModel,
        CommandFormModel commandFormModel, ProductFormModel productFormModel)
        {
            _createCommandUseCase = createCommandUseCase;
            _getListCollectionsUseCase = getListCollectionsUseCase;
            _createCollectionUseCase = createCollectionUseCase;
            _preferenceService = preferenceService;
            _deleteCollectionUseCase = deleteCollectionUseCase;
            ItemTappedCommand = new RelayCommand<ListCollection>(OnItemTapped);
            DeleteCollectionCommand = new RelayCommand<ListCollection>(DeleteCollection!);
            _collectionFormModel = collectionFormModel;
            _commandFormModel = commandFormModel;
            _productFormModel = productFormModel;
        }
        public ICommand ItemTappedCommand { get; }
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
        public async Task ShowBottomSheetAsync()
        {
            try
            {
                var viewModel = new BottomSheetPopupViewModel(
                    createCollectionUseCase: _createCollectionUseCase,
                    createCommandUseCase: _createCommandUseCase,
                    preferenceService: _preferenceService,
                    popup: new Popup(),
                    collectionViewModel: this,
                    commandViewModel: null,
                    isCollectionForm: true,
                    collectionFormModel: _collectionFormModel,
                    commandFormModel: null,
                    isProductForm: false,
                    productFormModel: null,
                    productViewModel: null,
                    createProductsUseCase: null);

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
                await Application.Current.MainPage.DisplayAlert("Exception", ex.Message, "OK");
                Console.WriteLine($"Exception: {ex}");
            }
        }



        private async void OnItemTapped(ListCollection collection)
        {
            if (collection == null)
                return;

            // Navigate to the ListNewCommandPage and pass the collection ID
            await Shell.Current.GoToAsync($"{nameof(ListNewCommandPage)}?CollectionId={collection.id}", true);
        }

        private async void DeleteCollection(ListCollection collection)
        {
            if (collection == null)
                return;
            var result = await _deleteCollectionUseCase.ExecuteAsync(collection.id);
            if (result)
            {
                await GetListCollectionAsync();
                await Shell.Current.DisplayAlert("Success", "Collection deleted", "OK");
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", "Failed to delete collection", "OK");
            }

        }
    }
}
