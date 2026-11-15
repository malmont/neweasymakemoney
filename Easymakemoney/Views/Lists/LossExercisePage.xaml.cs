namespace Easymakemoney.Views.Lists
{
    public partial class LossExercisePage : ContentPage
    {
        private readonly LossExerciseViewModel _viewModel;

        public LossExercisePage(LossExerciseViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadAsync();
        }
    }
}
