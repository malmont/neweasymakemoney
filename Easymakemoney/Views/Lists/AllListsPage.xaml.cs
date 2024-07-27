using Easymakemoney.ViewModels.Lists;

namespace Easymakemoney.Views.Lists;

public partial class AllListsPage : ContentPage
{
	public AllListsPage(AllListsViewModel viewModel)
	{
        this.BindingContext = viewModel;
        InitializeComponent();
	}
}
