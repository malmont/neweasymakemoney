
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

        public ListNewFraisDePortViewModel(GetListCollectionsUseCase getListCollectionsUseCase,
            GetListFraisDePortUseCase getListFraisDePortUseCase, DeleteFraisDePortUseCase deleteFraisDePortUseCase,
            SaveFraisDePortUseCase saveFraisDePortUseCase, FraisDePortFormModel fraisDePortFormModel,
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
            _getListFraisDePortUseCase = getListFraisDePortUseCase;
            _deleteFraisDePortUseCase = deleteFraisDePortUseCase;
            _saveFraisDePortUseCase = saveFraisDePortUseCase;
            _fraisDePortFormModel = fraisDePortFormModel;
            DeleteFraisDePortCommand = new RelayCommand<FraisDePort>(DeleteFraisDePort);
            ItemTappedFraisDePort = new RelayCommand<FraisDePort>(OnItemTapped);
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
                throw;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async void DeleteFraisDePort(FraisDePort fraisDePort)
        {
            if (IsBusy)
                return;
            IsBusy = true;
            try
            {
                await _deleteFraisDePortUseCase.ExecuteAsync(fraisDePort.id);
                ListFraisDePorts.Remove(fraisDePort);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async void OnItemTapped(FraisDePort fraisDePort)
        {
            if (fraisDePort == null)
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
                        this.CollectionImage = collection.Photo;
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
                        this.CommandImage = command.Photo;
                        await this.GetListFraisDePort();
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

            if (ListFraisDePorts.Count > 0)
            {
                await Shell.Current.DisplayAlert("Error", "Vous avez déjà un frais de port pour cette commande", "OK");
                return;
            }
            var viewModel = new BottomSheetPopupViewModel(
            popup: new Popup(),
            fraisDePortViewModel: this,
            isFraisDePortForm: true,
            saveFraisDePortUseCase: _saveFraisDePortUseCase,
            fraisDePortForm: _fraisDePortFormModel);

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


