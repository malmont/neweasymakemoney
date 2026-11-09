

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
        private readonly IDialogService _dialogService;

        public DashBoardCollectionViewModel(GetDashBoardCollectionUseCase getDashBoardCollectionUseCase,GetListCollectionsUseCase getListCollectionsUseCase,
            SaveCollectionUseCase saveCollectionUseCase, ListNewCollectionViewModel listNewCollectionViewModel,
             CollectionFormModel collectionFormModel,GetCollectionCloseUseCase getCollectionCloseUseCase,
             Easymakemoney.UseCase.PeriodResultUseCase.GetPeriodResultUseCase getPeriodResultUseCase,
             IDialogService dialogService)
        {
            _getPeriodResultUseCase = getPeriodResultUseCase;
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
                        this.CollectionId = collection.id;
                        this.CollectionImage = collection.Photo;
                        this.CollectionName = collection.Name;
                        this.IsClosed = collection.IsClosed;
                        this.IsPeriod = collection.IsPeriod;
                        this.IsReopened = collection.reopenedAt != null;
                        await this.GetDashBoardCollectionAsync(collection.id);
                        await this.LoadPeriodResultAsync();
                        IsClosedVisible=true;
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