namespace Easymakemoney.Views.Lists
{
    /// <summary>
    /// Paramètres : PeriodId (texte ou nombre) ; facultatifs PromotionPercent et ProductIds, posés par un conseil du guide
    /// (promotion sur des articles qui dorment).
    /// </summary>
    public partial class PeriodSimulationPage : ContentPage, IQueryAttributable
    {
        private readonly PeriodSimulationViewModel _viewModel;

        public PeriodSimulationPage(PeriodSimulationViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("PeriodId", out var period))
                _viewModel.PeriodId = period switch
                {
                    int id => id,
                    string text when int.TryParse(text, out var id) => id,
                    _ => 0,
                };

            if (query.TryGetValue("PromotionPercent", out var percent) && percent is double value && value > 0)
            {
                var productIds = query.TryGetValue("ProductIds", out var ids) && ids is IEnumerable<int> list ? list.ToList() : new List<int>();
                _viewModel.PresetPromotion(value, productIds);
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.StartAsync();
        }
    }
}
