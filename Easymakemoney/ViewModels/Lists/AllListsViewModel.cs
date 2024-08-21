
namespace Easymakemoney.ViewModels.Lists
{
    public partial class AllListsViewModel
	{
		public AllListsViewModel()
		{
            
        }
        [ICommand]
        async void NavigateToCollection()
        {
            await Shell.Current.GoToAsync(nameof(ListNewCollectionPage));
            //await Shell.Current.GoToAsync($"//{nameof(ListNewCollectionPage)}");
        }
        [ICommand]
        async void NavigateToNoteDeFrais()
        {
            await Shell.Current.GoToAsync(nameof(ListNewNoteDeFraisPage));
            //await Shell.Current.GoToAsync($"//{nameof(ListNewProductPage)}");
        }

        [ICommand]
          async void NavigateToNoteFournisseur()
        {
            await Shell.Current.GoToAsync(nameof(ListNewFournisseurPage));
            //await Shell.Current.GoToAsync($"//{nameof(ListNewProductPage)}");
        }

        [ICommand]
        async void NavigateToFraisDePort()
        {
            await Shell.Current.GoToAsync(nameof(ListNewFraisDePortPage));
            //await Shell.Current.GoToAsync($"//{nameof(ListNewProductPage)}");
        }
       
    }
}

