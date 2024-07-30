
namespace Easymakemoney.UseCases
{
    public class LoginUseCase
    {
        private readonly ILoginService _loginService;

        public LoginUseCase(ILoginService loginService)
        {
            _loginService = loginService;
        }

        public async Task<bool> ExecuteAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return false;

            var response = await _loginService.Authenticate(new LoginRequest
            {
                username = email,
                password = password
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

                return true;
            }

            return false;
        }
    }
}
