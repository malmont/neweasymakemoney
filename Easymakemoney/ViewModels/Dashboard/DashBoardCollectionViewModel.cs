

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

        public DashBoardCollectionViewModel(GetDashBoardCollectionUseCase getDashBoardCollectionUseCase,GetListCollectionsUseCase getListCollectionsUseCase,
            SaveCollectionUseCase saveCollectionUseCase, ListNewCollectionViewModel listNewCollectionViewModel,
             CollectionFormModel collectionFormModel,GetCollectionCloseUseCase getCollectionCloseUseCase)
        {
            _collectionFormModel = collectionFormModel;
            _listNewCollectionViewModel = listNewCollectionViewModel;
            _saveCollectionUseCase = saveCollectionUseCase;
            _getListCollectionsUseCase = getListCollectionsUseCase;
            _getDashBoardCollectionUseCase = getDashBoardCollectionUseCase;
            _getCollectionCloseUseCase = getCollectionCloseUseCase;
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
            set { _isClosed = value; OnPropertyChanged(nameof(IsClosed)); }
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
         [ICommand]
        public async Task CloseCollection()
        {
            if (IsBusy)
                return;

            IsBusy = true;
            try
            {
                var result = await _getCollectionCloseUseCase.GetCollectionClose(CollectionId);
                if (result != null)
                {
                    await Application.Current.MainPage.DisplayAlert("retour", result, "OK");
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

        


        public async Task GetDashBoardCollectionAsync(int CollectionId)
        {if (IsBusy)
                return;

            IsBusy = true;
            try
            {
                DashBoardCollection = await _getDashBoardCollectionUseCase.ExecuteAsync(CollectionId);
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
                        this.IsClosed = collection.IsClosed;
                        await this.GetDashBoardCollectionAsync(collection.id);
                        IsClosedVisible=true;
                    }
                });

            var popup = new BottomSheetPopupListView(viewModel);
            await Application.Current.MainPage.ShowPopupAsync(popup);
        }

         [ICommand]
        public async Task ShowBottomSheetCreateList()
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
            private async void ItemTapped()
        {
            if (CollectionId == 0)
                return;

            // Navigate to the ListNewCommandPage and pass the collection ID
            await Shell.Current.GoToAsync($"{nameof(DashBoardCommandPage)}?CollectionId={CollectionId}", true);
        }
     
    }
}