



namespace Easymakemoney.Views.Lists;

public partial class ListNewCollectionPage : ContentPage
{
    ListNewCollectionViewModel viewModel;

    public ListNewCollectionPage(ListNewCollectionViewModel viewModel)
	{
        this.viewModel = viewModel;
        this.BindingContext = viewModel;
        viewModel.ListCollections = new ObservableCollection<ListCollection>();
        InitializeComponent();
	}

    protected override async void OnAppearing()
    {
        await viewModel.GetRandomListCollectionAsync();

        //if (viewModel.GetRandomListCollectionAsync.CanExecute(null))
        //{
        //    await viewModel.GetRandomListCollectionAsync.ExecuteAsync(null);
        //    viewModel.FirstRun = false;
        //}

        base.OnAppearing();
    }

}
