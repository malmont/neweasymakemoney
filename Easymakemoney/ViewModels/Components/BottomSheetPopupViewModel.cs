using System.Windows.Input;
namespace Easymakemoney.ViewModels
{
    public partial class BottomSheetPopupViewModel : ObservableObject
    {

        private readonly SaveCommandUseCase _saveCommandUseCase;
        private readonly SaveProductUseCase _saveProductUseCase;
        private readonly SaveCollectionUseCase _saveCollectionUseCase;
        private readonly SaveProductvariantUseCase _saveProductvariantUseCase;

        private readonly ListNewCollectionViewModel _collectionViewModel;

        private readonly ListNewProductViewModel _productViewModel;

        private readonly ListNewCommandViewModel _commandViewModel;

        private readonly ListNewProductsVariantViewModel _productVariantViewModel;
        private Popup _popup;
        public BottomSheetPopupViewModel(
        Popup popup = null,
        bool isCollectionForm = false,
        bool isCommandForm = false,
        CollectionFormModel collectionFormModel = null,
        CommandFormModel commandFormModel = null,
        bool isProductForm = false,
        bool isProductVariantForm = false,
        ProductFormModel productFormModel = null,
        SaveCollectionUseCase saveCollectionUseCase = null,
        SaveCommandUseCase saveCommandUseCase = null,
        SaveProductUseCase saveProductUseCase = null
        , ListNewCollectionViewModel collectionViewModel = null,
        ListNewCommandViewModel commandViewModel = null,
        ListNewProductViewModel productViewModel = null,
        ListNewProductsVariantViewModel productVariantViewModel = null,
        SaveProductvariantUseCase saveProductvariantUseCase = null,
        ProductVariantFormModel productVariantForm = null)
        {
            IsProductVariantForm = isProductVariantForm;
            ProductVariantForm=productVariantForm;
            _productVariantViewModel = productVariantViewModel;
            _saveProductvariantUseCase = saveProductvariantUseCase;
            _productViewModel = productViewModel;
            _commandViewModel = commandViewModel;
            _collectionViewModel = collectionViewModel;
            _saveCollectionUseCase = saveCollectionUseCase;
            _saveCommandUseCase = saveCommandUseCase;
            _saveProductUseCase = saveProductUseCase;
            _popup = popup;
            IsCollectionForm = isCollectionForm;
            CollectionForm = collectionFormModel;
            IsCommandForm = isCommandForm;
            CommandForm = commandFormModel;
            IsProductForm = isProductForm;
            ProductForm = productFormModel;
            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        private bool _isCollectionForm;
        public bool IsCollectionForm
        {
            get => _isCollectionForm;
            set => SetProperty(ref _isCollectionForm, value);
        }

        private bool _isProductVariantForm;
        public bool IsProductVariantForm
        {
            get => _isProductVariantForm;
            set => SetProperty(ref _isProductVariantForm, value);
        }

        private bool _isCommandForm;
        public bool IsCommandForm
        {
            get => _isCommandForm;
            set => SetProperty(ref _isCommandForm, value);
        }

        private bool _isProductForm; // Ajouté pour les produits
        public bool IsProductForm
        {
            get => _isProductForm;
            set => SetProperty(ref _isProductForm, value);
        }



        public CollectionFormModel CollectionForm { get; }
        public CommandFormModel CommandForm { get; }

        public ProductFormModel ProductForm { get; }

        public ProductVariantFormModel ProductVariantForm { get; }

        public void SetPopupInstance(Popup popup) // Ajouter cette méthode pour recevoir l'instance du popup
        {
            _popup = popup;
        }
        private async void Save()
        {
            try
            {
                if (IsCollectionForm)
                {
                    var result = await _saveCollectionUseCase.ExecuteAsync(CollectionForm);
                    if (result) await _collectionViewModel.GetListCollectionAsync();
                    if (!result) throw new Exception("Failed to save collection.");
                }
                else if (IsCommandForm)
                {
                    var result = await _saveCommandUseCase.ExecuteAsync(CommandForm, _commandViewModel.CollectionId);
                    if (result) await _commandViewModel.GetListCommandAsync();
                    if (!result) throw new Exception("Failed to save command.");
                }
                else if (IsProductForm)
                {
                    var result = await _saveProductUseCase.ExecuteAsync(ProductForm, _productViewModel.CommandId);
                    if (result) await _productViewModel.GetListProductAsync();
                    if (!result) throw new Exception("Failed to save product.");
                }
                else if (IsProductVariantForm)
                {
                    var result = await _saveProductvariantUseCase.ExecuteAsync(ProductVariantForm, _productVariantViewModel.ProductId);
                    if (result) await _productVariantViewModel.GetlistProductVariantAsync();
                    if (!result) throw new Exception("Failed to save product variant.");
                }

                _popup.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
        }
        private void Cancel()
        {
            _popup.Close();
        }
    }
}
