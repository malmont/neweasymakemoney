using Easymakemoney.Services.Tenant;

namespace Easymakemoney.ViewModels
{
	public partial class AppShellViewModel:BaseViewModel
	{
		private readonly ISiteDataCleaner _siteDataCleaner;

		public AppShellViewModel(ISiteDataCleaner siteDataCleaner)
		{
			_siteDataCleaner = siteDataCleaner;
		}

		[RelayCommand]
		async Task ChangeSiteAsync()
		{
			await _siteDataCleaner.PromptAndChangeSiteAsync();
		}

		[RelayCommand]
		async void SignOut()
		{
            if (Preferences.ContainsKey(nameof(App.UserDetails)))
            {
                Preferences.Remove(nameof(App.UserDetails));
            };
            await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
		}
    }
}
