

using Easymakemoney.Services.Dialog;

namespace Easymakemoney.ViewModels.Dashboard
{
    public partial class DashBoardCollectionViewModel:BaseViewModel
    {
        private readonly GetDashBoardCollectionUseCase _getDashBoardCollectionUseCase;

        private readonly SaveCollectionUseCase _saveCollectionUseCase;

        private readonly GetListCollectionsUseCase _getListCollectionsUseCase;

        private readonly ListNewCollectionViewModel _listNewCollectionViewModel;

        private readonly GetCollectionCloseUseCase _getCollectionCloseUseCase;

        private readonly CollectionFormModel _collectionFormModel;
        private readonly Easymakemoney.UseCase.PeriodResultUseCase.GetPeriodResultUseCase _getPeriodResultUseCase;
        private readonly Easymakemoney.UseCase.PeriodResultUseCase.PeriodUseCase _periodUseCase;
        private readonly IDialogService _dialogService;

        public DashBoardCollectionViewModel(GetDashBoardCollectionUseCase getDashBoardCollectionUseCase,GetListCollectionsUseCase getListCollectionsUseCase,
            SaveCollectionUseCase saveCollectionUseCase, ListNewCollectionViewModel listNewCollectionViewModel,
             CollectionFormModel collectionFormModel,GetCollectionCloseUseCase getCollectionCloseUseCase,
             Easymakemoney.UseCase.PeriodResultUseCase.GetPeriodResultUseCase getPeriodResultUseCase,
             Easymakemoney.UseCase.PeriodResultUseCase.PeriodUseCase periodUseCase,
             IDialogService dialogService)
        {
            _getPeriodResultUseCase = getPeriodResultUseCase;
            _periodUseCase = periodUseCase;
            _collectionFormModel = collectionFormModel;
            _listNewCollectionViewModel = listNewCollectionViewModel;
            _saveCollectionUseCase = saveCollectionUseCase;
            _getListCollectionsUseCase = getListCollectionsUseCase;
            _getDashBoardCollectionUseCase = getDashBoardCollectionUseCase;
            _getCollectionCloseUseCase = getCollectionCloseUseCase;
            _dialogService = dialogService;
        }

       
        public DashBoardCollection _dashBoardCollection ;
        public DashBoardCollection DashBoardCollection
        {
            get => _dashBoardCollection;
            set { _dashBoardCollection = value; OnPropertyChanged(nameof(DashBoardCollection)); }
        }

        public int CollectionId { get; set; }

    
        public bool _isClosed = false;
        public bool IsClosed
        {
            get => _isClosed;
            set
            {
                _isClosed = value;
                OnPropertyChanged(nameof(IsClosed));
                OnPropertyChanged(nameof(CanClose));
                OnPropertyChanged(nameof(CanReopen));
                OnPropertyChanged(nameof(CanEditBudget));
            }
        }

        public bool _isClosedVisible = false;
        public bool IsClosedVisible
        {
            get => _isClosedVisible;
            set { _isClosedVisible = value; OnPropertyChanged(nameof(IsClosedVisible)); }
        }

         private string _collectionImage;
        public string CollectionImage
        {
            get => _collectionImage;
            set { _collectionImage = value; OnPropertyChanged(nameof(CollectionImage)); }
        }

        [ObservableProperty]
        private string? collectionName;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanClose))]
        [NotifyPropertyChangedFor(nameof(CanReopen))]
        [NotifyPropertyChangedFor(nameof(CloseButtonText))]
        [NotifyPropertyChangedFor(nameof(CanEditBudget))]
        private bool isPeriod;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanClose))]
        private bool isReopened;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasPeriodResult))]
        private PeriodResult? periodResult;

        public bool HasPeriodResult => PeriodResult != null;

        private async Task LoadPeriodResultAsync()
        {
            PeriodResult = null;
            if (!IsPeriod)
                return;

            try
            {
                PeriodResult = await _getPeriodResultUseCase.ExecuteAsync(CollectionId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await _dialogService.ShowErrorAsync(ex);
            }
        }

        // Une période se clôture seule : le bouton ne sert qu'aux collections manuelles et aux périodes rouvertes.
        public bool CanClose => !IsClosed && (!IsPeriod || IsReopened);
        public string CloseButtonText => IsPeriod ? "Clôturer la période" : "Clôturer la collection";
        public bool CanReopen => IsClosed && IsPeriod;
        public bool CanEditBudget => IsPeriod && !IsClosed;

        /// <summary>À l'ouverture de l'écran, sans choix préalable : la période en cours.</summary>
        public async Task SelectCurrentPeriodIfNoneAsync()
        {
            if (CollectionId != 0)
                return;

            try
            {
                var current = await _periodUseCase.GetCurrentPeriodAsync();
                if (current != null && CollectionId == 0)
                {
                    await SelectCollectionAsync(current);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private async Task SelectCollectionAsync(ListCollection collection)
        {
            CollectionId = collection.id;
            CollectionImage = collection.Photo;
            CollectionName = collection.Name;
            IsClosed = collection.IsClosed;
            IsPeriod = collection.IsPeriod;
            IsReopened = collection.reopenedAt != null;
            await GetDashBoardCollectionAsync(collection.id);
            await LoadPeriodResultAsync();
            IsClosedVisible = true;
        }

        [RelayCommand]
        private async Task OpenBudgetLines()
        {
            if (CollectionId <= 0 || !IsPeriod || Shell.Current is not { } shell)
                return;

            await shell.GoToAsync($"{nameof(PeriodBudgetPage)}?PeriodId={CollectionId}&ReadOnly={(IsClosed ? 1 : 0)}");
        }

        /// <summary>Simulateur : seulement pour une période ouverte (une période fermée n'a plus rien à décider).</summary>
        [RelayCommand]
        private async Task OpenSimulation()
        {
            if (CollectionId <= 0 || !CanEditBudget || Shell.Current is not { } shell)
                return;

            await shell.GoToAsync($"{nameof(PeriodSimulationPage)}?PeriodId={CollectionId}");
        }

        [RelayCommand]
        private async Task EditBudget()
        {
            var page = Application.Current?.MainPage;
            if (page == null || IsBusy)
                return;

            var text = await page.DisplayPromptAsync("Budget de la période", CollectionName, "Valider", "Annuler", keyboard: Keyboard.Numeric);
            if (string.IsNullOrWhiteSpace(text))
                return;

            // Accepte « 1 500,50 » comme « 1500.50 » (même lecture que les autres montants saisis).
            if (Easymakemoney.Converters.DecimalInputConverter.Parse(text) is not { } amount || amount < 0)
            {
                await _dialogService.ShowInfoAsync("Montant invalide.");
                return;
            }

            try
            {
                await _periodUseCase.UpdateBudgetAsync(CollectionId, amount);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await _dialogService.ShowErrorAsync(ex);
                return;
            }

            await GetDashBoardCollectionAsync(CollectionId);
        }

        [RelayCommand]
        public async Task ReopenCollection()
        {
            if (IsBusy)
                return;

            bool confirmed = await _dialogService.ConfirmAsync(
                "Rouvrir la période",
                "Les chiffres ne seront plus figés et de nouvelles saisies seront possibles.",
                "Rouvrir",
                "Annuler");
            if (!confirmed)
                return;

            IsBusy = true;
            try
            {
                await _getCollectionCloseUseCase.ReopenCollectionAsync(CollectionId);
                IsReopened = true;
                IsClosed = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await _dialogService.ShowErrorAsync(ex);
                return;
            }
            finally
            {
                IsBusy = false;
            }

            await GetDashBoardCollectionAsync(CollectionId);
            await LoadPeriodResultAsync();
        }
         [RelayCommand]
        public async Task CloseCollection()
        {
            if (IsBusy)
                return;

            IsBusy = true;
            try
            {
                await _getCollectionCloseUseCase.CloseCollectionAsync(CollectionId);
                await _dialogService.ShowInfoAsync(IsPeriod ? "Période clôturée" : "Collection clôturée");
                await GetDashBoardCollectionAsync(CollectionId);
                IsClosed = true;
                await LoadPeriodResultAsync();
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

        


        public async Task GetDashBoardCollectionAsync(int CollectionId)
        {
            if (IsBusy)
                return;

            IsBusy = true;
            try
            {
                DashBoardCollection = await _getDashBoardCollectionUseCase.ExecuteAsync(CollectionId);
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
        public async Task ShowBottomSheetPopupListView()
        {
            var viewModel = new BottomSheetPopupListViewViewModel(
                getItemsFunc: async () => await _getListCollectionsUseCase.ExecuteAsync(),
                onItemTappedAction: async (item) =>
                {
                    var collection = item as ListCollection;
                    if (collection != null)
                    {
                        await SelectCollectionAsync(collection);
                    }
                });

            var popup = new BottomSheetPopupListView(viewModel);
            if (Application.Current?.MainPage != null)
                await Application.Current.MainPage.ShowPopupAsync(popup);
        }

         [RelayCommand]
        public async Task ShowBottomSheetCreateList()
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
        private async Task ItemTapped()
        {
            if (CollectionId == 0)
                return;

            // Navigate to the ListNewCommandPage and pass the collection ID
            if (Shell.Current is { } shell)
                await shell.GoToAsync($"{nameof(DashBoardCommandPage)}?CollectionId={CollectionId}", true);
        }
     
    }
}