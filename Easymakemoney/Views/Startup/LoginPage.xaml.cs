using Easymakemoney.Services.Theme;

namespace Easymakemoney.Views.Startup;

public partial class LoginPage : ContentPage
{
    private readonly LoginPageViewModel _viewModel;

    // Initialisé avant InitializeComponent : les liaisons x:Reference de la page le lisent à la construction.
    public ISiteBrandingService? Branding { get; } = IPlatformApplication.Current?.Services.GetService<ISiteBrandingService>();

    public LoginPage(LoginPageViewModel viewModel)
    {
        InitializeComponent();
        this.BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}
