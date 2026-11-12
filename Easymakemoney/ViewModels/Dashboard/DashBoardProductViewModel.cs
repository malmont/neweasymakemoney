

using Easymakemoney.Services.Dialog;

namespace Easymakemoney.ViewModels.Dashboard
{
    public partial class DashBoardProductViewModel : BaseViewModel
    {

        private readonly ListNewProductViewModel _listNewProductViewModel;
        private readonly ProductFormModel _productFormModel;
        private readonly SaveProductUseCase _saveProductUseCase;
        private readonly GetListProductsUseCase _getListProductUseCase;
        private readonly IDialogService _dialogService;
        
        [ObservableProperty]
        private int displayedQuantity;

        public DashBoardProductViewModel(ListNewProductViewModel listNewProductViewModel, ProductFormModel productFormModel,
            SaveProductUseCase saveProductUseCase, GetListProductsUseCase getListProductUseCase, IDialogService dialogService)
        {
            _productFormModel = productFormModel;
            _getListProductUseCase = getListProductUseCase;
            _saveProductUseCase = saveProductUseCase;
            _listNewProductViewModel = listNewProductViewModel;
            _dialogService = dialogService;
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

        [ObservableProperty]
        private string? productName;

        [RelayCommand]
        public async Task ShowBottomSheetCreateProduct()
        {
            var viewModel = new BottomSheetPopupViewModel(
                popup: new Popup(),
                isProductForm: true,
                productViewModel: _listNewProductViewModel,
                saveProductUseCase: _saveProductUseCase,
                productFormModel: _productFormModel,
                dialogService: _dialogService
            );

            var popup = new BottomSheetPopup(viewModel);
            var mainPage = Easymakemoney.Utilities.AppPage.Current;

            if (mainPage != null)
            {
                await mainPage.ShowPopupAsync(popup);
            }
        }

        [RelayCommand]
        public async Task ShowBottomSheetPopupListProductView()
        {
            if (CommandId == 0)
            {
                await _dialogService.ShowInfoAsync("Vous devez choisir obligatoirement une commande");
                return;
            }
            var viewModel = new BottomSheetPopupListViewViewModel(
                getItemsFunc: async () =>
                {
                    try
                    {
                        return await _getListProductUseCase.ExecuteAsync(CommandId);
                    }
                    catch (Exception ex)
                    {
                        await _dialogService.ShowErrorAsync(ex);
                        return new ObservableCollection<ListProduct>();
                    }
                },
                onItemTappedAction: (item) =>
                {
                    var Product = item as ListProduct;
                    if (Product != null)
                    {
                        this.DashBoardProduct = Product;
                        this.ProductImage = Product.image;
                        this.ProductName = Product.Name;
                        DisplayedQuantity = Product.freezeQuantity ?? Product.quantity ?? 0;

                    }
                });

            var popup = new BottomSheetPopupListView(viewModel);
            if (Easymakemoney.Utilities.AppPage.Current != null)
                await Easymakemoney.Utilities.AppPage.Current!.ShowPopupAsync(popup);
        }
    }
}