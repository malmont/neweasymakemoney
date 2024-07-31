namespace Easymakemoney.Services
{
    public class LoginService : ILoginService
    {
        private const string LoginUrl = "https://backend-strapi.online/jeesign/api/login";
        private readonly IHttpService _httpService;

        public LoginService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<LoginResponse> Authenticate(LoginRequest loginRequest)
        {
            return await _httpService.PostAsync<LoginResponse>(LoginUrl, loginRequest);
        }
    }
}
