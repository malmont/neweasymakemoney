
namespace Easymakemoney.ViewModels.Lists
{
    public partial class AllListsViewModel
	{
		public AllListsViewModel()
		{
            
        }

        [RelayCommand]
        private async Task NavigateToCollection()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync(nameof(ListNewCollectionPage));
        }

        [RelayCommand]
        private async Task NavigateToNoteDeFrais()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync(nameof(ListNewNoteDeFraisPage));
        }

        [RelayCommand]
        private async Task NavigateToNoteFournisseur()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync(nameof(ListNewFournisseurPage));
        }

        [RelayCommand]
        private async Task NavigateToFraisDePort()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync(nameof(ListNewFraisDePortPage));
        }
    }
}

