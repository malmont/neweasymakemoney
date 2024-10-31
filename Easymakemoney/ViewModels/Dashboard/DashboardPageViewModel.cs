

using Easymakemoney.Views.Statistiques;

namespace Easymakemoney.ViewModels.Dashboard
{
    public partial class DashboardPageViewModel : BaseViewModel
    {
        public DashboardPageViewModel()
        {
            Title = "welcome";
        }

        [ICommand]
        async void NavigateToAllListsPage()
        {
            await Shell.Current.GoToAsync(nameof(AllListsPage));

        }

        [ICommand]
        async void NavigateTDashboardPage()
        {
           await Shell.Current.GoToAsync(nameof(DashBoardCollectionPage)); 
            //await Shell.Current.GoToAsync($"//{nameof(ListNewProductPage)}");
        }

        [ICommand]
        async void NavigateToStatistiquesPage()
        {
            await Shell.Current.GoToAsync(nameof(StatistiquesPage));
        }
    }
}

