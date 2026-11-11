using System.Globalization;

namespace Easymakemoney.Views.Statistiques
{
    [QueryProperty(nameof(AdviceId), "AdviceId")]
    [QueryProperty(nameof(ProductId), "ProductId")]
    [QueryProperty(nameof(ProductName), "ProductName")]
    [QueryProperty(nameof(Percent), "Percent")]
    [QueryProperty(nameof(Weekdays), "Weekdays")]
    public partial class RentalPromotionPage : ContentPage
    {
        private readonly RentalPromotionViewModel _viewModel;
        private int _adviceId;
        private int _productId;
        private string? _productName;
        private double? _percent;
        private string? _weekdays;
        private bool _initialized;

        public RentalPromotionPage(RentalPromotionViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        public string AdviceId { set => _adviceId = int.TryParse(value, out var id) ? id : 0; }
        public string ProductId { set => _productId = int.TryParse(value, out var id) ? id : 0; }
        public string ProductName { set => _productName = value; }
        public string Percent { set => _percent = double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var p) ? p : null; }
        public string Weekdays { set => _weekdays = value; }

        /// <summary>Initialisé une fois : un sélecteur de date qui se ferme ne remet pas le formulaire à zéro.</summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (_initialized)
                return;
            _initialized = true;
            _viewModel.Init(_adviceId, _productId, _productName, _percent, _weekdays);
            if (_adviceId > 0)
                await _viewModel.ComputePreviewCommand.ExecuteAsync(null);
        }
    }
}
