
namespace Easymakemoney.ViewModels.Lists
{
    public partial class AllListsViewModel
	{
		public AllListsViewModel()
		{
            
        }
        [RelayCommand]
        async void NavigateToCollection()
        {
            await Shell.Current.GoToAsync(nameof(ListNewCollectionPage));
            //await Shell.Current.GoToAsync($"//{nameof(ListNewCollectionPage)}");
        }
        [RelayCommand]
        async void NavigateToNoteDeFrais()
        {
            await Shell.Current.GoToAsync(nameof(ListNewNoteDeFraisPage));
            //await Shell.Current.GoToAsync($"//{nameof(ListNewProductPage)}");
        }

        [RelayCommand]
          async void NavigateToNoteFournisseur()
        {
            await Shell.Current.GoToAsync(nameof(ListNewFournisseurPage));
            //await Shell.Current.GoToAsync($"//{nameof(ListNewProductPage)}");
        }

        [RelayCommand]
        async void NavigateToFraisDePort()
        {
            await Shell.Current.GoToAsync(nameof(ListNewFraisDePortPage));
            //await Shell.Current.GoToAsync($"//{nameof(ListNewProductPage)}");
        }

        
       
    }
}

