namespace Easymakemoney.ViewModels.Startup
{
    public partial class LoginPageViewModel : BaseViewModel
    {
        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        private readonly LoginUseCase _loginUseCase;

        public LoginPageViewModel(LoginUseCase loginUseCase)
        {
            _loginUseCase = loginUseCase;
        }

        #region Commands
        [ICommand]
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
        #endregion
    }
}
