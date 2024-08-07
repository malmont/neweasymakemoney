
namespace Easymakemoney.Views.Lists;

public partial class ListNewCollectionPage : ContentPage
{
    private readonly ListNewCollectionViewModel _viewModel;

    public ListNewCollectionPage(ListNewCollectionViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.GetListCollectionAsync();
    }

}
