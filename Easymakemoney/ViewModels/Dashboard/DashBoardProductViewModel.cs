

namespace Easymakemoney.ViewModels.Dashboard
{
    public partial class DashBoardProductViewModel : BaseViewModel
    {

        private readonly ListNewProductViewModel _listNewProductViewModel;
        private readonly ProductFormModel _productFormModel;
        private readonly SaveProductUseCase _saveProductUseCase;
        private readonly GetListProductsUseCase _getListProductUseCase;

        public DashBoardProductViewModel(ListNewProductViewModel listNewProductViewModel, ProductFormModel productFormModel,
            SaveProductUseCase saveProductUseCase, GetListProductsUseCase getListProductUseCase)
        {
            _productFormModel = productFormModel;
            _getListProductUseCase = getListProductUseCase;
            _saveProductUseCase = saveProductUseCase;
            _listNewProductViewModel = listNewProductViewModel;


        }

        public ListProduct _dashBoardProduct;
        public ListProduct DashBoardProduct
        {
            get => _dashBoardProduct;
            set { _dashBoardProduct = value; OnPropertyChanged(nameof(DashBoardProduct)); }
        }

        public int ProductId { get; set; }

        public int CommandId { get; set; }

        private string _productImage;

        public string ProductImage
        {
            get => _productImage;
            set { _productImage = value; OnPropertyChanged(nameof(ProductImage)); }
        }
      
        [ICommand]
        public async Task ShowBottomSheetCreateProduct()
        {
            var viewModel = new BottomSheetPopupViewModel(
            popup: new Popup(),
            isProductForm: true,
            productViewModel: _listNewProductViewModel,
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

        [ICommand]
        public async Task ShowBottomSheetPopupListProductView()
        {
            if (CommandId == 0)
            {
                await Shell.Current.DisplayAlert("Error", "Vous devez choisir obligatoirement une commande", "OK");
                return;
            }
            var viewModel = new BottomSheetPopupListViewViewModel(
                getItemsFunc: async () => await _getListProductUseCase.ExecuteAsync(CommandId),
                onItemTappedAction:  (item) =>
                {
                    var Product = item as ListProduct;
                    if (Product != null)
                    {
                        this.DashBoardProduct = Product;
                        this.ProductImage = Product.image;
                      
                    }
                });

            var popup = new BottomSheetPopupListView(viewModel);
            await Application.Current.MainPage.ShowPopupAsync(popup);
        }
    }
}