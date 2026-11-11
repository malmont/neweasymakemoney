namespace Easymakemoney.Views.Lists
{
    [QueryProperty(nameof(AdviceId), "AdviceId")]
    public partial class AdvicePage : ContentPage
    {
        private readonly AdviceViewModel _viewModel;

        public AdvicePage(AdviceViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        public string AdviceId
        {
            set => _viewModel.FocusAdviceId = int.TryParse(value, out var id) ? id : 0;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadAsync();
            await _viewModel.LoadAiAsync();
        }
    }
}
