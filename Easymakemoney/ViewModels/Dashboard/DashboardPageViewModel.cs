using Microsoft.Maui.Controls;
using System.Windows.Input;

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
    }
}

