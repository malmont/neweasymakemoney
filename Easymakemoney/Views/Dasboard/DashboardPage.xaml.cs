namespace Easymakemoney.Views.Dasboard;

public partial class DashboardPage : ContentPage
{
    private readonly DashboardPageViewModel _viewModel;

    public DashboardPage(DashboardPageViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        this.BindingContext = viewModel;

    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadTilesAsync();
    }
}