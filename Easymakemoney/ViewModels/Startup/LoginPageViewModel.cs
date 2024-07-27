namespace Easymakemoney.ViewModels.Startup
{
    public partial class LoginPageViewModel : BaseViewModel
    {
        [ObservableProperty]
        private string _email;

        [ObservableProperty]
        private string _password;

        private readonly ILoginService _loginService;

        public LoginPageViewModel(ILoginService loginService)
        {
            _loginService = loginService;
        }

        #region Commands
        [ICommand]
        async Task LoginAsync()
        {
            if (!string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Password))
            {
                var response = await _loginService.Authenticate(new LoginRequest
                {
                    username = Email,
                    password = Password
                });

                if (response != null)
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jsonToken = handler.ReadJwtToken(response.Token) as JwtSecurityToken;

                    var username = jsonToken.Claims.First(claim => claim.Type == "username").Value;
                    var role = jsonToken.Claims.First(claim => claim.Type == "roles").Value;

                    string encryptedToken = EncryptionHelper.Encrypt(response.Token);
                    Microsoft.Maui.Storage.Preferences.Set("userToken", encryptedToken);

                    UserBasicInfo userDetails = new UserBasicInfo()
                    {
                        Email = username,
                        Role = role
                    };

                    string userDetailStr = JsonConvert.SerializeObject(userDetails);
                    Microsoft.Maui.Storage.Preferences.Set(nameof(App.UserDetails), userDetailStr);
                    App.UserDetails = userDetails;
                    App.Token = response.Token;
                    AppShell.Current.FlyoutHeader = new FlyoutHeaderControl();
                    await Shell.Current.GoToAsync("//DashboardPage");
                }
                else
                {
                    await AppShell.Current.DisplayAlert("Invalid User", "Invalid User", "OK");
                }
            }
        }
        #endregion
    }
}
