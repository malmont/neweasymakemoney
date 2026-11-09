using Easymakemoney.Services.Tenant;

namespace Easymakemoney.ViewModels.Startup
{
    public partial class LoginPageViewModel : BaseViewModel
    {
        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        private readonly LoginUseCase _loginUseCase;
        private readonly ISiteDataCleaner _siteDataCleaner;

        public LoginPageViewModel(LoginUseCase loginUseCase, ISiteDataCleaner siteDataCleaner)
        {
            _loginUseCase = loginUseCase;
            _siteDataCleaner = siteDataCleaner;
        }

        #region Commands
        [RelayCommand]
        async Task LoginAsync()
        {
            if (await _loginUseCase.ExecuteAsync(Email, Password))
            {
                // Successfully logged in
            }
            else
            {
                await AppShell.Current.DisplayAlert("Invalid User", "Invalid User", "OK");
            }
        }

        [RelayCommand]
        async Task ChangeSiteAsync()
        {
            await _siteDataCleaner.PromptAndChangeSiteAsync();
        }
        #endregion
    }
}
