namespace Easymakemoney.Views.Lists
{
    [QueryProperty(nameof(CommandeId), "CommandeId")]
    public partial class ReceptionLabelsPage : ContentPage
    {
        private readonly ReceptionLabelsViewModel _viewModel;

        public ReceptionLabelsPage(ReceptionLabelsViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        public string CommandeId
        {
            set => _viewModel.CommandeId = int.TryParse(value, out var id) ? id : 0;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadAsync();
        }
    }
}
