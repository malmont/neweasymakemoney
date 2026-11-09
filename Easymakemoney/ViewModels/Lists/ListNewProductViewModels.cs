using System.Windows.Input;
using Easymakemoney.Services.Dialog;

namespace Easymakemoney.ViewModels.Lists
{
    public partial class ListNewProductViewModel : BaseViewModel
    {
        private readonly GetListProductsUseCase _getListProductsUseCase;
        private readonly DeleteProductsUseCase _deleteProductUseCase;
        private readonly ProductFormModel _productFormModel;

        private readonly SaveProductUseCase _saveProductUseCase;
        private readonly IDialogService _dialogService;

        public ObservableCollection<ListProduct> ListProducts { get; set; } = new ObservableCollection<ListProduct>();

        public ListNewProductViewModel(GetListProductsUseCase getListProductsUseCase, ProductFormModel productFormModel, 
        SaveProductUseCase saveProductUseCase, DeleteProductsUseCase deleteProductUseCase,
        IDialogService dialogService)
        {
            _saveProductUseCase = saveProductUseCase;
            _getListProductsUseCase = getListProductsUseCase;
            _deleteProductUseCase = deleteProductUseCase;
            _productFormModel = productFormModel;
            _dialogService = dialogService;
            DeleteProductCommand = new AsyncRelayCommand<ListProduct>(DeleteProduct);
            ItemTappedProductsVariants = new AsyncRelayCommand<ListProduct>(OnItemTapped);
        }
        public ICommand ItemTappedProductsVariants { get; }

        public int CommandId { get; set; }
        public ICommand DeleteProductCommand { get; }
        public async Task GetListProductAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;
            ListProducts.Clear();
            try
            {
                var products = await _getListProductsUseCase.ExecuteAsync(CommandId);

                foreach (var product in products)
                {
                    ListProducts.Add(product);
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
                isProductForm: true,
                productViewModel: this,
                saveProductUseCase: _saveProductUseCase,
                productFormModel: _productFormModel,
                dialogService: _dialogService
            );

            var popup = new BottomSheetPopup(viewModel);
            var mainPage = Application.Current?.MainPage;

            if (mainPage != null)
            {
                await mainPage.ShowPopupAsync(popup);
            }
        }
        private async Task OnItemTapped (ListProduct? product)
        {
            if (product == null)
                return;

            // Navigate to the ListNewCommandPage and pass the collection ID
            if (Shell.Current is { } shell)
                await shell.GoToAsync($"{nameof(ListNewProductVariantPage)}?ProductId={product.id}", true);
        }

        private async Task DeleteProduct(ListProduct? product)
        {
            if (product == null)
                return;

            if (IsBusy)
                return;

            bool confirmed = await _dialogService.ConfirmAsync(
                "Confirmation",
                "Êtes-vous sûr de vouloir supprimer ce produit ?",
                "Supprimer",
                "Annuler");
            if (!confirmed)
                return;

            IsBusy = true;
            try
            {
                var result = await _deleteProductUseCase.ExecuteAsync(product.id);
                if (result)
                {
                    ListProducts.Remove(product);
                    await _dialogService.ShowInfoAsync("Produit supprimé avec succès.");
                }
                else
                {
                    await _dialogService.ShowInfoAsync("Échec de la suppression du produit.");
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