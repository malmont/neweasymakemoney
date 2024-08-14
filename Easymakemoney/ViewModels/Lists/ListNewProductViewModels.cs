using System.Windows.Input;
using Easymakemoney.Components;

namespace Easymakemoney.ViewModels.Lists
{
    public partial class ListNewProductViewModel : BaseViewModel
    {
        private readonly GetListProductsUseCase _getListProductsUseCase;
        private readonly CreateProductsUseCase _createProductUseCase;
        // private readonly DeleteProductUseCase _deleteProductUseCase;
        private readonly IPreferenceService _preferenceService;
        private readonly ProductFormModel _productFormModel;

        public ObservableCollection<ListProduct> ListProducts { get; set; } = new ObservableCollection<ListProduct>();

        public ListNewProductViewModel(GetListProductsUseCase getListProductsUseCase, IPreferenceService preferenceService,
        CreateProductsUseCase createProductUseCase, ProductFormModel productFormModel)
        {
            _getListProductsUseCase = getListProductsUseCase;
            _createProductUseCase = createProductUseCase;
            _preferenceService = preferenceService;
            // _deleteProductUseCase = deleteProductUseCase;
            _productFormModel = productFormModel;
            // DeleteProductCommand = new RelayCommand<ListProduct>(DeleteProduct);
        }

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
            createCollectionUseCase: null,
            createCommandUseCase: null,
            preferenceService: _preferenceService,
            popup: new Popup(),
            collectionViewModel: null,
            commandViewModel: null,
            isCollectionForm: false,
            isCommandForm: false,
            collectionFormModel: null,
            commandFormModel: null,
            isProductForm: true,
            productFormModel: _productFormModel, 
            productViewModel: this,
            createProductsUseCase: _createProductUseCase
        );

            var popup = new BottomSheetPopup(viewModel);
            var mainPage = Application.Current?.MainPage;

            if (mainPage != null)
            {
                await mainPage.ShowPopupAsync(popup);
            }
        }

    }
}