using Easymakemoney.Models.Login;
using Easymakemoney.Services;
using Easymakemoney.Services.Auth;
using Easymakemoney.Services.Session;
using Easymakemoney.Services.Tenant;

namespace Easymakemoney.UseCases
{
    public class LoginUseCase
    {
        private readonly ILoginService _loginService;
        private readonly IJwtService _jwtService;
        private readonly ITenantStore _tenantStore;
        private readonly ISessionService _sessionService;
        private readonly ISessionExpiryNotifier _sessionExpiryNotifier;

        public LoginUseCase(
            ILoginService loginService,
            IJwtService jwtService,
            ITenantStore tenantStore,
            ISessionService sessionService,
            ISessionExpiryNotifier sessionExpiryNotifier)
        {
            _loginService = loginService;
            _jwtService = jwtService;
            _tenantStore = tenantStore;
            _sessionService = sessionService;
            _sessionExpiryNotifier = sessionExpiryNotifier;
        }

        public async Task<LoginResult> ExecuteLoginAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return LoginResult.Failure("Veuillez saisir votre adresse e-mail et votre mot de passe.");
            }

            var (response, error) = await _loginService.AuthenticateAsync(email.Trim(), password);

            if (error != null)
            {
                // Règle M5 : PLATFORM_NOT_ALLOWED affiche toujours le texte français de la consigne
                if (string.Equals(error.Code, "PLATFORM_NOT_ALLOWED", StringComparison.OrdinalIgnoreCase))
                {
                    return LoginResult.Failure("Ce compte n'a pas accès à l'application de gestion.", error);
                }

                return LoginResult.Failure(error.Message, error);
            }

            if (response == null)
            {
                return LoginResult.Failure("Réponse du serveur invalide.");
            }

            // Cas normal administrateur : double facteur exigé
            if (response.OtpRequired)
            {
                return LoginResult.RequireOtp(email.Trim());
            }

            // Cas sans double facteur : jetons retournés directement
            return await ProcessSuccessfulTokensAsync(response.Token, response.RefreshToken, email.Trim());
        }

        public async Task<LoginResult> ExecuteVerifyOtpAsync(string email, string otp)
        {
            if (string.IsNullOrWhiteSpace(otp))
            {
                return LoginResult.Failure("Veuillez saisir le code reçu par courriel.");
            }

            var (response, error) = await _loginService.VerifyOtpAsync(email.Trim(), otp.Trim());

            if (error != null)
            {
                // Règle M5 : PLATFORM_NOT_ALLOWED affiche toujours le texte français de la consigne
                if (string.Equals(error.Code, "PLATFORM_NOT_ALLOWED", StringComparison.OrdinalIgnoreCase))
                {
                    return LoginResult.Failure("Ce compte n'a pas accès à l'application de gestion.", error);
                }

                return LoginResult.Failure(error.Message, error);
            }

            if (response == null)
            {
                return LoginResult.Failure("Réponse du serveur invalide.");
            }

            return await ProcessSuccessfulTokensAsync(response.Token, response.RefreshToken, email.Trim());
        }

        private async Task<LoginResult> ProcessSuccessfulTokensAsync(string? token, string? refreshToken, string? email = null)
        {
            // Règle M6 : si l'un des jetons est vide, réponse incomplète
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(refreshToken))
            {
                return LoginResult.Failure("Réponse du serveur incomplète.");
            }

            var tenant = await _tenantStore.GetTenantAsync();
            if (tenant == null)
            {
                return LoginResult.Failure("Aucun site n'est configuré.");
            }

            // Contrôle strict du JWT (ROLE_ADMIN et tenant_code)
            if (!_jwtService.TryParseAndValidate(token, tenant.Code, out var user, out var errorMessage))
            {
                return LoginResult.Failure(errorMessage ?? "Le jeton d'authentification est invalide.");
            }

            // Enregistrement de la session sécurisée
            await _sessionService.SaveSessionAsync(token, refreshToken, user!);

            // Règle N6 : retenir la dernière adresse e-mail dans SecureStorage
            var effectiveEmail = !string.IsNullOrWhiteSpace(user?.Email) ? user.Email : email;
            if (!string.IsNullOrWhiteSpace(effectiveEmail))
            {
                await _sessionService.SaveLastLoginEmailAsync(effectiveEmail);
            }

            _sessionExpiryNotifier.Reset();

            return LoginResult.Success(user!);
        }
    }
}
