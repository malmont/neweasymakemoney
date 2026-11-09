
using Easymakemoney.Services.Dialog;

namespace Easymakemoney.ViewModels.Lists
{
    public partial class ListNewFraisDePortViewModel : BaseViewModel
    {
        private readonly GetListFraisDePortUseCase _getListFraisDePortUseCase;

        private readonly DeleteFraisDePortUseCase _deleteFraisDePortUseCase;

        private readonly FraisDePortFormModel _fraisDePortFormModel;

        private readonly SaveFraisDePortUseCase _saveFraisDePortUseCase;
        private readonly GetListCollectionsUseCase _getListCollectionsUseCase;

        private readonly CollectionFormModel _collectionFormModel;

        private readonly ListNewCollectionViewModel _listNewCollectionViewModel;

        private readonly SaveCollectionUseCase _saveCollectionUseCase;

        private readonly ListNewCommandViewModel _listNewCommandViewModel;

        private readonly SaveCommandUseCase _saveCommandUseCase;

        private readonly GetListCommandUseCase _getListCommandUseCase;

        private readonly CommandFormModel _commandFormModel;
        private readonly IDialogService _dialogService;

        public ListNewFraisDePortViewModel(GetListCollectionsUseCase getListCollectionsUseCase,
            GetListFraisDePortUseCase getListFraisDePortUseCase, DeleteFraisDePortUseCase deleteFraisDePortUseCase,
            SaveFraisDePortUseCase saveFraisDePortUseCase, FraisDePortFormModel fraisDePortFormModel,
            SaveCollectionUseCase saveCollectionUseCase, ListNewCollectionViewModel listNewCollectionViewModel,
            CollectionFormModel collectionFormModel, ListNewCommandViewModel listNewCommandViewModel,
            SaveCommandUseCase saveCommandUseCase, GetListCommandUseCase getListCommandUseCase,
            CommandFormModel commandFormModel, IDialogService dialogService)
        {
            _commandFormModel = commandFormModel;
            _getListCommandUseCase = getListCommandUseCase;
            _saveCommandUseCase = saveCommandUseCase;
            _listNewCommandViewModel = listNewCommandViewModel;
            _collectionFormModel = collectionFormModel;
            _listNewCollectionViewModel = listNewCollectionViewModel;
            _saveCollectionUseCase = saveCollectionUseCase;
            _getListCollectionsUseCase = getListCollectionsUseCase;
            _getListFraisDePortUseCase = getListFraisDePortUseCase;
            _deleteFraisDePortUseCase = deleteFraisDePortUseCase;
            _saveFraisDePortUseCase = saveFraisDePortUseCase;
            _fraisDePortFormModel = fraisDePortFormModel;
            _dialogService = dialogService;
            DeleteFraisDePortCommand = new AsyncRelayCommand<FraisDePort>(DeleteFraisDePort);
            ItemTappedFraisDePort = new AsyncRelayCommand<FraisDePort>(OnItemTapped);
        }
        public ObservableCollection<FraisDePort> ListFraisDePorts { get; set; } = new ObservableCollection<FraisDePort>();

        public ICommand ItemTappedFraisDePort { get; }

        public int CollectionId { get; set; }

        public int CommandId { get; set; }



        private string _collectionImage;
        public string CollectionImage
        {
            get => _collectionImage;
            set { _collectionImage = value; OnPropertyChanged(nameof(CollectionImage)); }
        }

        private string _commandImage;

        public string CommandImage
        {
            get => _commandImage;
            set { _commandImage = value; OnPropertyChanged(nameof(CommandImage)); }
        }


        public ICommand DeleteFraisDePortCommand { get; }

        public async Task GetListFraisDePort()
        {
            if (IsBusy)
                return;

            ListFraisDePorts.Clear();

            try
            {
                var listFraisDePort = await _getListFraisDePortUseCase.ExecuteAsync(CommandId);

                if (listFraisDePort != null)
                {
                    foreach (var fraisDePort in listFraisDePort)
                    {
                        if (fraisDePort != null)
                        {
                            ListFraisDePorts.Add(fraisDePort);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await _dialogService.ShowErrorAsync(e);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task DeleteFraisDePort(FraisDePort? fraisDePort)
        {
            if (fraisDePort == null)
                return;

            if (IsBusy)
                return;

            bool confirmed = await _dialogService.ConfirmAsync(
                "Confirmation",
                "Êtes-vous sûr de vouloir supprimer ce frais de port ?",
                "Supprimer",
                "Annuler");
            if (!confirmed)
                return;

            IsBusy = true;
            try
            {
                var result = await _deleteFraisDePortUseCase.ExecuteAsync(CommandId);
                if (result)
                {
                    ListFraisDePorts.Remove(fraisDePort);
                    await _dialogService.ShowInfoAsync("Frais de port supprimé avec succès.");
                }
                else
                {
                    await _dialogService.ShowInfoAsync("Échec de la suppression du frais de port.");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await _dialogService.ShowErrorAsync(e);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task OnItemTapped(FraisDePort? fraisDePort)
        {
            if (fraisDePort == null)
                return;
            await Task.CompletedTask;
        }


        [RelayCommand]
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
                        this.CollectionImage = collection.Photo;
                        // await this.GetListFournisseursAsync();
                    }
                });

            var popup = new BottomSheetPopupListView(viewModel);
            if (Application.Current?.MainPage != null)
                await Application.Current.MainPage.ShowPopupAsync(popup);
        }

        [RelayCommand]
        public async Task ShowBottomSheetPopupListCommandView()
        {
            if (CollectionId == 0)
            {
                await _dialogService.ShowInfoAsync("Vous devez choisir obligatoirement une collection");
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
                        this.CommandImage = command.Photo;
                        await this.GetListFraisDePort();
                    }
                });

            var popup = new BottomSheetPopupListView(viewModel);
            if (Application.Current?.MainPage != null)
                await Application.Current.MainPage.ShowPopupAsync(popup);
        }


        [RelayCommand]
        public async Task ShowBottomSheet()
        {
            if (CollectionId == 0)
            {
                await _dialogService.ShowInfoAsync("Vous devez choisir obligatoirement une collection");
                return;
            }
            if (CommandId == 0)
            {
                await _dialogService.ShowInfoAsync("Vous devez choisir obligatoirement une commande");
                return;
            }

            if (ListFraisDePorts.Count > 0)
            {
                await _dialogService.ShowInfoAsync("Un frais de port existe déjà pour cette commande.");
                return;
            }
            var viewModel = new BottomSheetPopupViewModel(
                popup: new Popup(),
                fraisDePortViewModel: this,
                isFraisDePortForm: true,
                saveFraisDePortUseCase: _saveFraisDePortUseCase,
                fraisDePortForm: _fraisDePortFormModel,
                dialogService: _dialogService);

            var popup = new BottomSheetPopup(viewModel);
            var mainPage = Application.Current?.MainPage;

            if (mainPage != null)
            {
                await mainPage.ShowPopupAsync(popup);
            }
        }

        [RelayCommand]
        public async Task ShowBottomSheetCreateCollection()
        {
            try
            {
                var viewModel = new BottomSheetPopupViewModel(
                    popup: new Popup(),
                    saveCollectionUseCase: _saveCollectionUseCase,
                    isCollectionForm: true,
                    collectionViewModel: _listNewCollectionViewModel,
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

        [RelayCommand]
        public async Task ShowBottomSheetCreateCommand()
        {
            if (CollectionId == 0)
            {
                await _dialogService.ShowInfoAsync("Vous devez choisir obligatoirement une collection");
                return;
            }
            _listNewCommandViewModel.CollectionId = CollectionId;
            var viewModel = new BottomSheetPopupViewModel(
                popup: new Popup(),
                commandViewModel: _listNewCommandViewModel,
                isCommandForm: true,
                saveCommandUseCase: _saveCommandUseCase,
                commandFormModel: _commandFormModel,
                dialogService: _dialogService);

            var popup = new BottomSheetPopup(viewModel);
            var mainPage = Application.Current?.MainPage;

            if (mainPage != null)
            {
                await mainPage.ShowPopupAsync(popup);
            }
        }




    }
}


