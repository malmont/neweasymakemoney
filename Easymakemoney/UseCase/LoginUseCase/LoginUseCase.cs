
namespace Easymakemoney.UseCases
{
    public class LoginUseCase
    {
        private readonly ILoginService _loginService;
        private readonly IJwtService _jwtService;
        private readonly IPreferenceService _preferenceService;

        public LoginUseCase(ILoginService loginService, IJwtService jwtService, IPreferenceService preferenceService)
        {
            _loginService = loginService;
            _jwtService = jwtService;
            _preferenceService = preferenceService;
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
                var (username, role,userId) = _jwtService.ParseToken(response.Token);
                _preferenceService.SaveUserDetails(response.Token, username, role, userId);

                AppShell.Current.FlyoutHeader = new FlyoutHeaderControl();
                await Shell.Current.GoToAsync("//DashboardPage");

                return true;
            }

            return false;
        }
    }
}
