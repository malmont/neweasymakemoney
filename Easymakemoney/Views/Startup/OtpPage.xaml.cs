using Easymakemoney.Services.Theme;
using Easymakemoney.ViewModels.Startup;

namespace Easymakemoney.Views.Startup;

public partial class OtpPage : ContentPage
{
    private readonly OtpPageViewModel _viewModel;

    // Initialisé avant InitializeComponent : les liaisons x:Reference de la page le lisent à la construction.
    public ISiteBrandingService? Branding { get; } = IPlatformApplication.Current?.Services.GetService<ISiteBrandingService>();

    public OtpPage(OtpPageViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}

