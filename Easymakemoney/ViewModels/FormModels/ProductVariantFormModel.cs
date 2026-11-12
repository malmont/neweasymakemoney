// Code antérieur aux annotations de nullité : avertissements coupés ici, sans effet sur l'exécution.
#nullable disable warnings




namespace Easymakemoney.ViewModels.FormModels
{
    public class ProductVariantFormModel : ObservableObject
    {
        public ObservableCollection<ColorsProduct> Colors { get; set; } = new ObservableCollection<ColorsProduct>();

        private ColorsProduct _selectedColor;
        public ColorsProduct SelectedColor
        {
            get => _selectedColor;
            set { _selectedColor = value; OnPropertyChanged(nameof(SelectedColor)); }
        }

        public ObservableCollection<SizeProduct> Sizes { get; set; } = new ObservableCollection<SizeProduct>();

        private SizeProduct _selectedSize;
        public SizeProduct SelectedSize
        {
            get => _selectedSize;
            set { _selectedSize = value; OnPropertyChanged(nameof(SelectedSize)); }
        }

        private int _stockQuantity;
        public int StockQuantity
        {
            get => _stockQuantity;
            set { _stockQuantity = value; OnPropertyChanged(nameof(StockQuantity)); }
        }

        private readonly GetListColorsUseCase _getListColorsUseCase;
        private readonly GetListSizesUseCase _getListSizesUseCase;

        public ProductVariantFormModel(GetListColorsUseCase getListColorsUseCase, GetListSizesUseCase getListSizesUseCase)
        {
            _getListColorsUseCase = getListColorsUseCase;
            _getListSizesUseCase = getListSizesUseCase;
            LoadColors();
            LoadSizes();
        }

        private async void LoadColors()
        {
            try
            {
                // Call API to get categories and populate the Categories collection
                var colors = await _getListColorsUseCase.GetListColors();
                Colors.Clear();
                foreach (var color in colors)
                {
                    Colors.Add(color);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadColors failed: {ex.Message}");
            }
        }

        private async void LoadSizes()
        {
            try
            {
                // Call API to get categories and populate the Categories collection
                var sizes = await _getListSizesUseCase.GetListSizes();
                Sizes.Clear();
                foreach (var size in sizes)
                {
                    Sizes.Add(size);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadSizes failed: {ex.Message}");
            }
        }
    }
}
