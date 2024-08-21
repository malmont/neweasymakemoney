

namespace Easymakemoney.ViewModels.Lists
{
    public partial class ListNewFournisseurViewModel : BaseViewModel
    {
        private readonly GetListFournisseursUseCase _getListFournisseurUseCase;

        private readonly DeleteFournisseurUseCase _deleteFournisseurUseCase;

        private readonly FournisseurFormModel _fournisseurFormModel;

        private readonly SaveFournisseurUseCase _saveFournisseurUseCase;


        public ListNewFournisseurViewModel(GetListFournisseursUseCase getListFournisseurUseCase, DeleteFournisseurUseCase deleteFournisseurUseCase,
            SaveFournisseurUseCase saveFournisseurUseCase, FournisseurFormModel fournisseurFormModel
        )
        {

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
                var fournisseurs = await _getListFournisseurUseCase.ExecuteAsync();

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
        public async Task ShowBottomSheet()
        {
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

     


    }
}