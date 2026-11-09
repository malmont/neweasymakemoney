using Easymakemoney.ViewModels.Startup;

namespace Easymakemoney.Views.Startup;

public partial class ActivationPage : ContentPage
{
    public ActivationPage(ActivationPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

