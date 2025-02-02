namespace Easymakemoney.Services
{
    public class LoginService : ILoginService
    {
        private const string LoginUrl = Configurations.BackendSymfonyUrl;
        private readonly IHttpService _httpService;

        public LoginService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<LoginResponse> Authenticate(LoginRequest loginRequest)
        {
            var url = $"{LoginUrl}/login";
            return await _httpService.PostAsyncWithoutAuth<LoginResponse>(url, loginRequest);
        }
    }
}
