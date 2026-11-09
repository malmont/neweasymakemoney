using Easymakemoney.ViewModels.Startup;

namespace Easymakemoney.Views.Startup;

public partial class OtpPage : ContentPage
{
    private readonly OtpPageViewModel _viewModel;

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

