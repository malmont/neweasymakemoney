

using Easymakemoney.Views.Statistiques;

namespace Easymakemoney.ViewModels.Dashboard
{
    public partial class DashboardPageViewModel : BaseViewModel
    {
        public DashboardPageViewModel()
        {
            Title = "welcome";
        }

        [RelayCommand]
        private async Task NavigateToAllListsPage()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync(nameof(AllListsPage));
        }

        [RelayCommand]
        private async Task NavigateTDashboardPage()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync(nameof(DashBoardCollectionPage)); 
        }

        [RelayCommand]
        private async Task NavigateToStatistiquesPage()
        {
            if (Shell.Current is { } shell)
                await shell.GoToAsync(nameof(StatistiquesPage));
        }
    }
}

