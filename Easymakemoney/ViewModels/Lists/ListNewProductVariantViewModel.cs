using System.Windows.Input;
using Easymakemoney.Services.Dialog;

namespace Easymakemoney.ViewModels.Lists
{
    public partial class ListNewProductsVariantViewModel : BaseViewModel
    {
        public ListNewProductsVariantViewModel(GetListProductsVariantUseCase getListProductsVariantUseCase, DeleteProductsVariantUseCase deleteProductsVariantUseCase
          , SaveProductvariantUseCase saveProductvariantUseCase, ProductVariantFormModel productVariantFormModel,
          IDialogService dialogService)
        {
            _getListProductsVariantUseCase = getListProductsVariantUseCase;
            _deleteProductsVariantUseCase = deleteProductsVariantUseCase;
            _saveProductvariantUseCase = saveProductvariantUseCase;
            _productVariantFormModel = productVariantFormModel;
            _dialogService = dialogService;
            DeleteProductVariantCommand = new AsyncRelayCommand<ProductVariant>(DeleteProductVariant);
        }


        private readonly GetListProductsVariantUseCase _getListProductsVariantUseCase;
        private readonly DeleteProductsVariantUseCase _deleteProductsVariantUseCase;
        private readonly ProductVariantFormModel _productVariantFormModel;

        private readonly SaveProductvariantUseCase _saveProductvariantUseCase;
        private readonly IDialogService _dialogService;

        public ObservableCollection<ProductVariant> ListProductVariants { get; set; } = new ObservableCollection<ProductVariant>();



        public int ProductId { get; set; }
        public ICommand DeleteProductVariantCommand { get; }

        public async Task GetlistProductVariantAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;
            ListProductVariants.Clear();
            try
            {
                var productVariants = await _getListProductsVariantUseCase.ExecuteAsync(ProductId);

                foreach (var productVariant in productVariants)
                {
                    ListProductVariants.Add(productVariant);
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

        [RelayCommand]
        public async Task ShowBottomSheet()
        {
            var viewModel = new BottomSheetPopupViewModel(
                popup: new Popup(),
                productVariantViewModel: this,
                isProductVariantForm: true,
                productVariantForm: _productVariantFormModel,
                saveProductvariantUseCase: _saveProductvariantUseCase,
                dialogService: _dialogService);

            var popup = new BottomSheetPopup(viewModel);
            var mainPage = Easymakemoney.Utilities.AppPage.Current;

            if (mainPage != null)
            {
                await mainPage.ShowPopupAsync(popup);
            }
        }

        private async Task DeleteProductVariant(ProductVariant? productVariant)
        {
            if (productVariant == null)
                return;

            if (IsBusy)
                return;

            bool confirmed = await _dialogService.ConfirmAsync(
                "Confirmation",
                "Êtes-vous sûr de vouloir supprimer cette variante ?",
                "Supprimer",
                "Annuler");
            if (!confirmed)
                return;

            IsBusy = true;
            try
            {
                var result = await _deleteProductsVariantUseCase.ExecuteAsync(productVariant.id);
                if (result)
                {
                    ListProductVariants.Remove(productVariant);
                    await _dialogService.ShowInfoAsync("Variante supprimée avec succès.");
                }
                else
                {
                    await _dialogService.ShowInfoAsync("Échec de la suppression de la variante.");
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


    }
}
