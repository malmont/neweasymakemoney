using System.Windows.Input;

namespace Easymakemoney.ViewModels.Lists
{
    public partial class ListNewNoteDeFraisViewModel : BaseViewModel
    {
        private readonly GetListNoteDeFraisUseCase _getListNoteDeFraisUseCase;
        private readonly DeleteNoteDeFraisUseCase _deleteNoteDeFraisUseCase;
        private readonly NoteDeFraisForModel _noteDeFraisForModel;

        private readonly GetListCollectionsUseCase _getListCollectionsUseCase;

        private readonly CollectionFormModel _collectionFormModel;

        private readonly ListNewCollectionViewModel _listNewCollectionViewModel;

        private readonly SaveCollectionUseCase _saveCollectionUseCase;

        private readonly SaveNoteDeFraisUseCase _saveNoteDeFraisUseCase;

        public ListNewNoteDeFraisViewModel(GetListCollectionsUseCase getListCollectionsUseCase,
         GetListNoteDeFraisUseCase getListNoteDeFraisUseCase, DeleteNoteDeFraisUseCase deleteNoteDeFraisUseCase,
          SaveNoteDeFraisUseCase saveNoteDeFraisUseCase, NoteDeFraisForModel noteDeFraisForModel, SaveCollectionUseCase saveCollectionUseCase,
          ListNewCollectionViewModel listNewCollectionViewModel, CollectionFormModel collectionFormModel)
        {
            _collectionFormModel = collectionFormModel;
            _listNewCollectionViewModel = listNewCollectionViewModel;
            _saveCollectionUseCase = saveCollectionUseCase;
            _getListCollectionsUseCase = getListCollectionsUseCase;
            _getListNoteDeFraisUseCase = getListNoteDeFraisUseCase;
            _deleteNoteDeFraisUseCase = deleteNoteDeFraisUseCase;
            _saveNoteDeFraisUseCase = saveNoteDeFraisUseCase;
            _noteDeFraisForModel = noteDeFraisForModel;
            DeleteNoteDefraisCommand = new RelayCommand<NoteDeFrais>(DeleteNoteDeFrais);
            ItemTappedNoteDeFrais = new RelayCommand<NoteDeFrais>(OnItemTapped);

        }

        public ObservableCollection<NoteDeFrais> ListNoteDeFrais { get; set; } = new ObservableCollection<NoteDeFrais>();


        public ICommand ItemTappedNoteDeFrais { get; }
        public int CollectionId { get; set; }
        public ICommand DeleteNoteDefraisCommand { get; }

        public async Task GetListNoteDeFraisAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;
            ListNoteDeFrais.Clear();
            try
            {
                var noteDeFrais = await _getListNoteDeFraisUseCase.ExecuteAsync(CollectionId);

                foreach (var note in noteDeFrais)
                {
                    ListNoteDeFrais.Add(note);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await Shell.Current.DisplayAlert("Error", "Unable to get note de frais", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async void DeleteNoteDeFrais(NoteDeFrais noteDeFrais)
        {
            if (noteDeFrais == null)
                return;

            if (await _deleteNoteDeFraisUseCase.ExecuteAsync(noteDeFrais.id))
            {
                ListNoteDeFrais.Remove(noteDeFrais);
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", "Unable to delete note de frais", "OK");
            }
        }

        private async void OnItemTapped(NoteDeFrais noteDeFrais)
        {
            if (noteDeFrais == null)
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
                        await this.GetListNoteDeFraisAsync();
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
            var viewModel = new BottomSheetPopupViewModel(
            popup: new Popup(),
            noteDeFraisViewModel: this,
            isNoteDeFraisForm: true,
            saveNoteDeFraisUseCase: _saveNoteDeFraisUseCase,
            noteDeFraisForm: _noteDeFraisForModel);

            var popup = new BottomSheetPopup(viewModel);
            var mainPage = Application.Current?.MainPage;

            if (mainPage != null)
            {
                await mainPage.ShowPopupAsync(popup);
            }
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




    }


}