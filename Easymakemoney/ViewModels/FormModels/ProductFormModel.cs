using System.Windows.Input;
using Easymakemoney.UseCase.ListCategoryUseCase;
using EEasymakemoney.UseCase.ListStyleUsesCase;


namespace Easymakemoney.ViewModels.FormModels
{
    public class ProductFormModel : ObservableObject
    {
        private string _name;
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        private string _description;
        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(nameof(Description)); }
        }

        private double _purchasePrice;
        public double PurchasePrice
        {
            get => _purchasePrice;
            set { _purchasePrice = value; OnPropertyChanged(nameof(PurchasePrice)); }
        }

        private int _coefficientMultiplier;
        public int CoefficientMultiplier
        {
            get => _coefficientMultiplier;
            set { _coefficientMultiplier = value; OnPropertyChanged(nameof(CoefficientMultiplier)); }
        }

        private Stream _imageStream;
        public Stream ImageStream
        {
            get => _imageStream;
            set { _imageStream = value; OnPropertyChanged(nameof(ImageStream)); }
        }

        private string _imageFileName;
        public string ImageFileName
        {
            get => _imageFileName;
            set { _imageFileName = value; OnPropertyChanged(nameof(ImageFileName)); }
        }

        private string _imagePath;
        public string ImagePath
        {
            get => _imagePath;
            set { _imagePath = value; OnPropertyChanged(nameof(ImagePath)); }
        }

        public ObservableCollection<Category> Categories { get; set; } = new ObservableCollection<Category>();

        private Category _selectedCategory;
        public Category SelectedCategory
        {
            get => _selectedCategory;
            set { _selectedCategory = value; OnPropertyChanged(nameof(SelectedCategory)); }
        }

        public ObservableCollection<Styles> Styles { get; set; } = new ObservableCollection<Styles>();

        private Styles _selectedStyles;
        public Styles SelectedStyles
        {
            get => _selectedStyles;
            set { _selectedStyles = value; OnPropertyChanged(nameof(SelectedStyles)); }
        }
        private readonly GetListCategoryUseCase _getListCategoryUseCase;
        private readonly GetListStyleUsesCases _getListStyleUseCase;

        public ICommand SelectImageCommand { get; }

        public ProductFormModel(GetListCategoryUseCase getListCategoryUseCase, GetListStyleUsesCases getListStyleUseCase)
        {
            _getListStyleUseCase = getListStyleUseCase;
            _getListCategoryUseCase = getListCategoryUseCase;
            SelectImageCommand = new Command(OnSelectImage);
            LoadCategories();
            LoadStyles();
        }

        private async void LoadCategories()
        {
            // Call API to get categories and populate the Categories collection
            var categories = await _getListCategoryUseCase.ExecuteAsync();
            Categories.Clear();
            foreach (var category in categories)
            {
                Categories.Add(category);
            }
        }

        private async void LoadStyles()
        {
          
            var styles = await _getListStyleUseCase.ExecuteAsync();
            Styles.Clear();
            foreach (var style in styles)
            {
                Styles.Add(style);
            }
        }

        private async void OnSelectImage()
        {
            try
            {
                var result = await FilePicker.PickAsync(new PickOptions
                {
                    FileTypes = FilePickerFileType.Images,
                    PickerTitle = "Please select an image"
                });

                if (result != null)
                {
                    ImageFileName = result.FileName;
                    ImagePath = result.FullPath;
                    ImageStream = await result.OpenReadAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Image selection failed: {ex.Message}");
            }
        }

        // Convert to ListProduct
        public ListProduct ToListProduct()
        {
            return new ListProduct
            {
                name = this.Name,
                description = this.Description,
                purchasePrice = this.PurchasePrice,
                coefficientMultiplier = this.CoefficientMultiplier,
                category = this.SelectedCategory != null ? new List<Category> { this.SelectedCategory } : new List<Category>(),
                style = this.SelectedStyles != null ? this.SelectedStyles : new Styles(),
            };
        }
    }
}
