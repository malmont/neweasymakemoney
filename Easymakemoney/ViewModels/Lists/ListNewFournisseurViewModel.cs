

namespace Easymakemoney.ViewModels.Lists
{
    public partial class ListNewFournisseurViewModel : BaseViewModel
    {
        private readonly GetListFournisseursUseCase _getListFournisseurUseCase;

        private readonly DeleteFournisseurUseCase _deleteFournisseurUseCase;

        private readonly FournisseurFormModel _fournisseurFormModel;

        private readonly GetListCollectionsUseCase _getListCollectionsUseCase;

        private readonly CollectionFormModel _collectionFormModel;

        private readonly ListNewCollectionViewModel _listNewCollectionViewModel;

        private readonly SaveCollectionUseCase _saveCollectionUseCase;

        private readonly SaveFournisseurUseCase _saveFournisseurUseCase;

        private readonly ListNewCommandViewModel _listNewCommandViewModel;

        private readonly SaveCommandUseCase _saveCommandUseCase;

        private readonly GetListCommandUseCase _getListCommandUseCase;

        private readonly CommandFormModel _commandFormModel;

        public ListNewFournisseurViewModel(GetListCollectionsUseCase getListCollectionsUseCase,
            GetListFournisseursUseCase getListFournisseurUseCase, DeleteFournisseurUseCase deleteFournisseurUseCase,
            SaveFournisseurUseCase saveFournisseurUseCase, FournisseurFormModel fournisseurFormModel,
            SaveCollectionUseCase saveCollectionUseCase, ListNewCollectionViewModel listNewCollectionViewModel,
            CollectionFormModel collectionFormModel, ListNewCommandViewModel listNewCommandViewModel,
            SaveCommandUseCase saveCommandUseCase, GetListCommandUseCase getListCommandUseCase,
            CommandFormModel commandFormModel)
        {
            _commandFormModel = commandFormModel;
            _getListCommandUseCase = getListCommandUseCase;
            _saveCommandUseCase = saveCommandUseCase;
            _listNewCommandViewModel = listNewCommandViewModel;
            _collectionFormModel = collectionFormModel;
            _listNewCollectionViewModel = listNewCollectionViewModel;
            _saveCollectionUseCase = saveCollectionUseCase;
            _getListCollectionsUseCase = getListCollectionsUseCase;
            _getListFournisseurUseCase = getListFournisseurUseCase;
            _deleteFournisseurUseCase = deleteFournisseurUseCase;
            _saveFournisseurUseCase = saveFournisseurUseCase;
            _fournisseurFormModel = fournisseurFormModel;
            DeleteFournisseurCommand = new RelayCommand<ListFournisseur>(DeleteFournisseur);
            ItemTappedFournisseur = new RelayCommand<ListFournisseur>(OnItemTapped);
        }

        public ObservableCollection<ListFournisseur> ListFournisseurs { get; set; } = new ObservableCollection<ListFournisseur>();

        public ICommand ItemTappedFournisseur { get; }

        public int CollectionId { get; set; }

        public int CommandId { get; set; }

        public ICommand DeleteFournisseurCommand { get; }

        public async Task GetListFournisseursAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;
            ListFournisseurs.Clear();
            try
            {
                var fournisseurs = await _getListFournisseurUseCase.ExecuteAsync(CommandId);

                foreach (var fournisseur in fournisseurs)
                {
                    ListFournisseurs.Add(fournisseur);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async void DeleteFournisseur(ListFournisseur fournisseur)
        {
            if (IsBusy)
                return;

            IsBusy = true;
            try
            {
                var result = await _deleteFournisseurUseCase.ExecuteAsync(fournisseur.id);
                if (result)
                {
                    ListFournisseurs.Remove(fournisseur);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async void OnItemTapped(ListFournisseur fournisseur)
        {
            if (fournisseur == null)
                return;
        }
        [ICommand]
        public async Task ShowBottomSheetPopupListView()
        {
            var viewModel = new BottomSheetPopupListViewViewModel(
                getItemsFunc: async () => await _getListCollectionsUseCase.ExecuteAsync(),
                onItemTappedAction: async (item) =>
                {
                    var collection = item as ListCollection;
                    if (collection != null)
                    {
                        this.CollectionId = collection.id;
                        // await this.GetListFournisseursAsync();
                    }
                });

            var popup = new BottomSheetPopupListView(viewModel);
            await Application.Current.MainPage.ShowPopupAsync(popup);
        }

        [ICommand]
        public async Task ShowBottomSheetPopupListCommandView()
        {
             if (CollectionId == 0)
            {
                await Shell.Current.DisplayAlert("Error", "Vous devez choisir obligatoirement une collection", "OK");
                return;
            }
            var viewModel = new BottomSheetPopupListViewViewModel(
                getItemsFunc: async () => await _getListCommandUseCase.ExecuteAsync(CollectionId),
                onItemTappedAction: async (item) =>
                {
                    var command = item as ListCommand;
                    if (command != null)
                    {
                        this.CommandId = command.id;
                        await this.GetListFournisseursAsync();
                    }
                });

            var popup = new BottomSheetPopupListView(viewModel);
            await Application.Current.MainPage.ShowPopupAsync(popup);
        }


        [ICommand]
        public async Task ShowBottomSheet()
        {
            if (CollectionId == 0)
            {
                await Shell.Current.DisplayAlert("Error", "Vous devez choisir obligatoirement une collection", "OK");
                return;
            }
             if (CommandId == 0)
            {
                await Shell.Current.DisplayAlert("Error", "Vous devez choisir obligatoirement une commande", "OK");
                return;
            }
            var viewModel = new BottomSheetPopupViewModel(
            popup: new Popup(),
            fournisseurViewModel: this,
            isFournisseurForm: true,
            saveFournisseurUseCase: _saveFournisseurUseCase,
            fournisseurForm: _fournisseurFormModel);

            var popup = new BottomSheetPopup(viewModel);
            var mainPage = Application.Current?.MainPage;

            if (mainPage != null)
            {
                await mainPage.ShowPopupAsync(popup);
            }
        }

        [ICommand]
        public async Task ShowBottomSheetCreateCollection()
        {
            try
            {
                var viewModel = new BottomSheetPopupViewModel(
                    popup: new Popup(),
                    saveCollectionUseCase: _saveCollectionUseCase,
                    isCollectionForm: true,
                    collectionViewModel: _listNewCollectionViewModel,
                    collectionFormModel: _collectionFormModel);

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
                if (Application.Current?.MainPage != null)
                    await Application.Current.MainPage.DisplayAlert("Exception", ex.Message, "OK");
                Console.WriteLine($"Exception: {ex}");
            }
        }

         [ICommand]
        public async Task ShowBottomSheetCreateCommand()
        {
             if (CollectionId == 0)
            {
                await Shell.Current.DisplayAlert("Error", "Vous devez choisir obligatoirement une collection", "OK");
                return;
            }
            _listNewCommandViewModel.CollectionId = CollectionId;
            var viewModel = new BottomSheetPopupViewModel(
            popup: new Popup(),
            commandViewModel: _listNewCommandViewModel,
            isCommandForm: true,
            saveCommandUseCase: _saveCommandUseCase,
            commandFormModel: _commandFormModel);

            var popup = new BottomSheetPopup(viewModel);
            var mainPage = Application.Current?.MainPage;

            if (mainPage != null)
            {
                await mainPage.ShowPopupAsync(popup);
            }
        }




    }
}