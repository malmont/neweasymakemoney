using System;
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
    }
}

