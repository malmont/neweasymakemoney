

using Easymakemoney.Services.Dialog;

namespace Easymakemoney.ViewModels.Lists
{
    public partial class ListNewFournisseurViewModel : BaseViewModel
    {
        private readonly GetListFournisseursUseCase _getListFournisseurUseCase;

        private readonly DeleteFournisseurUseCase _deleteFournisseurUseCase;

        private readonly FournisseurFormModel _fournisseurFormModel;

        private readonly SaveFournisseurUseCase _saveFournisseurUseCase;
        private readonly IDialogService _dialogService;


        public ListNewFournisseurViewModel(GetListFournisseursUseCase getListFournisseurUseCase, DeleteFournisseurUseCase deleteFournisseurUseCase,
            SaveFournisseurUseCase saveFournisseurUseCase, FournisseurFormModel fournisseurFormModel,
            IDialogService dialogService)
        {

            _getListFournisseurUseCase = getListFournisseurUseCase;
            _deleteFournisseurUseCase = deleteFournisseurUseCase;
            _saveFournisseurUseCase = saveFournisseurUseCase;
            _fournisseurFormModel = fournisseurFormModel;
            _dialogService = dialogService;
            DeleteFournisseurCommand = new AsyncRelayCommand<ListFournisseur>(DeleteFournisseur);
            ItemTappedFournisseur = new AsyncRelayCommand<ListFournisseur>(OnItemTapped);
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
                await _dialogService.ShowErrorAsync(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task DeleteFournisseur(ListFournisseur? fournisseur)
        {
            if (fournisseur == null)
                return;

            if (IsBusy)
                return;

            bool confirmed = await _dialogService.ConfirmAsync(
                "Confirmation",
                "Êtes-vous sûr de vouloir supprimer ce fournisseur ?",
                "Supprimer",
                "Annuler");
            if (!confirmed)
                return;

            IsBusy = true;
            try
            {
                var result = await _deleteFournisseurUseCase.ExecuteAsync(fournisseur.id);
                if (result)
                {
                    ListFournisseurs.Remove(fournisseur);
                    await _dialogService.ShowInfoAsync("Fournisseur supprimé avec succès.");
                }
                else
                {
                    await _dialogService.ShowInfoAsync("Échec de la suppression du fournisseur.");
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

        private async Task OnItemTapped(ListFournisseur? fournisseur)
        {
            if (fournisseur == null)
                return;
            await Task.CompletedTask;
        }
      

        [RelayCommand]
        public async Task ShowBottomSheet()
        {
            var viewModel = new BottomSheetPopupViewModel(
                popup: new Popup(),
                fournisseurViewModel: this,
                isFournisseurForm: true,
                saveFournisseurUseCase: _saveFournisseurUseCase,
                fournisseurForm: _fournisseurFormModel,
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