namespace Easymakemoney.Views.Lists
{
    /// <summary>Paramètres facultatifs : Kind (period | year), PeriodId, Year ; ils lancent la demande d'un bilan à l'ouverture.</summary>
    public partial class ReportsPage : ContentPage, IQueryAttributable
    {
        private readonly ReportsViewModel _viewModel;

        public ReportsPage(ReportsViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (!query.TryGetValue("Kind", out var kindValue) || kindValue?.ToString() is not { } kind)
                return;
            int? Read(string key) => query.TryGetValue(key, out var v) && int.TryParse(v?.ToString(), out var n) ? n : null;
            if (kind == "period" && Read("PeriodId") is { } periodId)
                _viewModel.PendingRequest = ("period", periodId);
            else if (kind == "year")
                _viewModel.PendingRequest = ("year", Read("Year"));
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadAsync();
        }
    }
}
