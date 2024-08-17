using System.Windows.Input;
namespace Easymakemoney.ViewModels.Lists
{
    public partial class ListNewProductsVariantViewModel : BaseViewModel
    {
        public ListNewProductsVariantViewModel(GetListProductsVariantUseCase getListProductsVariantUseCase, DeleteProductsVariantUseCase deleteProductsVariantUseCase
          , SaveProductvariantUseCase saveProductvariantUseCase, ProductVariantFormModel productVariantFormModel)
        {
            _getListProductsVariantUseCase = getListProductsVariantUseCase;
            _deleteProductsVariantUseCase = deleteProductsVariantUseCase;
            _saveProductvariantUseCase = saveProductvariantUseCase;
            _productVariantFormModel = productVariantFormModel;
            DeleteProductVariantCommand = new RelayCommand<ProductVariant>(DeleteProductVariant);
        }


        private readonly GetListProductsVariantUseCase _getListProductsVariantUseCase;
        private readonly DeleteProductsVariantUseCase _deleteProductsVariantUseCase;
        private readonly ProductVariantFormModel _productVariantFormModel;

        private readonly SaveProductvariantUseCase _saveProductvariantUseCase;

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
                await Shell.Current.DisplayAlert("Error", "Unable to get product variants", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [ICommand]
        public async Task ShowBottomSheet()
        {
            var viewModel = new BottomSheetPopupViewModel(
                popup: new Popup(),
                productVariantViewModel: this,
                isProductVariantForm: true,
                productVariantForm: _productVariantFormModel,
                saveProductvariantUseCase: _saveProductvariantUseCase);

            var popup = new BottomSheetPopup(viewModel);
            var mainPage = Application.Current?.MainPage;

            if (mainPage != null)
            {
                await mainPage.ShowPopupAsync(popup);
            }
        }

        private async void DeleteProductVariant(ProductVariant productVariant)
        {
            if (IsBusy)
                return;

            IsBusy = true;
            try
            {
                var result = await _deleteProductsVariantUseCase.ExecuteAsync(productVariant.id);
                if (result)
                {
                    ListProductVariants.Remove(productVariant);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await Shell.Current.DisplayAlert("Error", "Unable to delete product variant", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }


    }
}
