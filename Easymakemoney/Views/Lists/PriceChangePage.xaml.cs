using System.Globalization;

namespace Easymakemoney.Views.Lists
{
    [QueryProperty(nameof(AdviceId), "AdviceId")]
    [QueryProperty(nameof(CommandeId), "CommandeId")]
    [QueryProperty(nameof(Percent), "Percent")]
    [QueryProperty(nameof(ProductId), "ProductId")]
    public partial class PriceChangePage : ContentPage
    {
        private readonly PriceChangeViewModel _viewModel;
        private int _adviceId;
        private int _commandeId;
        private int _productId;
        private double? _percent;
        private bool _initialized;

        public PriceChangePage(PriceChangeViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        public string AdviceId { set => _adviceId = int.TryParse(value, out var id) ? id : 0; }
        public string CommandeId { set => _commandeId = int.TryParse(value, out var id) ? id : 0; }
        public string ProductId { set => _productId = int.TryParse(value, out var id) ? id : 0; }
        public string Percent { set => _percent = double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var p) ? p : null; }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (_initialized)
                return;
            _initialized = true;
            _viewModel.Init(_adviceId, _commandeId, _percent, _productId);
            await _viewModel.ComputePreviewCommand.ExecuteAsync(null);
        }
    }
}
