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
        // W2-APPRENDRE : lexique du serveur en arrière-plan (au plus toutes les 30 minutes) ; le glossaire local reste le secours.
        _ = IPlatformApplication.Current?.Services.GetService<Services.ILexiconService>()?.RefreshAsync();
        await _viewModel.LoadTilesAsync();
    }
}