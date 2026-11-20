

namespace Easymakemoney.Views.Lists;

public partial class AllListsPage : ContentPage
{
	private readonly AllListsViewModel _viewModel;

	public AllListsPage(AllListsViewModel viewModel)
	{
        this.BindingContext = _viewModel = viewModel;
        InitializeComponent();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		// Tuile des transferts du groupe : relue à chaque retour sur l'écran.
		await _viewModel.LoadGroupAsync();
	}
}
