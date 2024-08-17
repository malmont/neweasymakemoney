using System.Windows.Input;


namespace Easymakemoney.ViewModels.Lists
{
    public partial class ListNewProductViewModel : BaseViewModel
    {
        private readonly GetListProductsUseCase _getListProductsUseCase;
        private readonly DeleteProductsUseCase _deleteProductUseCase;
        private readonly ProductFormModel _productFormModel;

        private readonly SaveProductUseCase _saveProductUseCase;

        public ObservableCollection<ListProduct> ListProducts { get; set; } = new ObservableCollection<ListProduct>();

        public ListNewProductViewModel(GetListProductsUseCase getListProductsUseCase, ProductFormModel productFormModel, 
        SaveProductUseCase saveProductUseCase, DeleteProductsUseCase deleteProductUseCase)
        {
            _saveProductUseCase = saveProductUseCase;
            _getListProductsUseCase = getListProductsUseCase;
            _deleteProductUseCase = deleteProductUseCase;
            _productFormModel = productFormModel;
            DeleteProductCommand = new RelayCommand<ListProduct>(DeleteProduct);
            ItemTappedProductsVariants = new RelayCommand<ListProduct>(OnItemTapped);
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
                await Shell.Current.DisplayAlert("Error", "Unable to get products", "OK");
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
            isProductForm: true,
            productViewModel: this,
            saveProductUseCase: _saveProductUseCase,
            productFormModel: _productFormModel
        );

            var popup = new BottomSheetPopup(viewModel);
            var mainPage = Application.Current?.MainPage;

            if (mainPage != null)
            {
                await mainPage.ShowPopupAsync(popup);
            }
        }
        private async void OnItemTapped (ListProduct product)
        {
          if (product == null)
                return;

            // Navigate to the ListNewCommandPage and pass the collection ID
            await Shell.Current.GoToAsync($"{nameof(ListNewProductVariantPage)}?ProductId={product.id}", true);
        }

        private async void DeleteProduct(ListProduct product)
        {
            if (IsBusy)
                return;

            IsBusy = true;
            try
            {
                var result = await _deleteProductUseCase.ExecuteAsync(product.id);
                if (result)
                {
                    ListProducts.Remove(product);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await Shell.Current.DisplayAlert("Error", "Unable to delete product", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

    }
}