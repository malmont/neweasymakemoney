using System.Diagnostics;
using System.Net;
using System.Text;
using Easymakemoney.Models.Login;
using Easymakemoney.Services.Session;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Easymakemoney.Services.Auth;

public class TokenRefresher : ITokenRefresher
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISessionService _sessionService;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public TokenRefresher(
        IHttpClientFactory httpClientFactory,
        ISessionService sessionService)
    {
        _httpClientFactory = httpClientFactory;
        _sessionService = sessionService;
    }

    public async Task<RefreshResult> RefreshAsync(string? originalToken, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // Vérifier si le jeton a déjà été renouvelé par une autre requête pendant l'attente du sémaphore
            var currentToken = await _sessionService.GetTokenAsync();
            if (!string.IsNullOrWhiteSpace(currentToken) &&
                !string.IsNullOrWhiteSpace(originalToken) &&
                !string.Equals(currentToken, originalToken, StringComparison.Ordinal))
            {
                // Un rafraîchissement a déjà réussi, on retourne directement le nouveau jeton
                return RefreshResult.Success(currentToken);
            }

            var refreshToken = await _sessionService.GetRefreshTokenAsync();
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                // Pas de refresh token : session terminée (effacement sans notification sous sémaphore, règle N3)
                await _sessionService.ClearSessionAsync();
                return RefreshResult.Rejected(HttpStatusCode.Unauthorized);
            }

            var payload = new RefreshTokenRequest
            {
                refresh_token = refreshToken,
                platform = "mobile"
            };

            var json = JsonConvert.SerializeObject(payload);
            using var request = new HttpRequestMessage(HttpMethod.Post, "token/refresh");
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var client = _httpClientFactory.CreateClient("auth");

            HttpResponseMessage response;
            string content;
            try
            {
                response = await client.SendAsync(request, cancellationToken);
                using (response)
                {
                    content = await response.Content.ReadAsStringAsync(cancellationToken);
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                // Serveur injoignable : on garde la session
                Debug.WriteLine($"[TokenRefresher] Erreur réseau lors du rafraîchissement : {ex.Message}");
                return RefreshResult.NetworkError();
            }

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var jObj = JObject.Parse(content);
                    var newToken = jObj["token"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(newToken))
                    {
                        await _sessionService.UpdateTokenAsync(newToken);
                        return RefreshResult.Success(newToken);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[TokenRefresher] Erreur parsing réponse rafraîchissement : {ex.Message}");
                }

                // Réponse 200 sans jeton
                await _sessionService.ClearSessionAsync();
                return RefreshResult.Rejected(response.StatusCode);
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.BadRequest)
            {
                // Refus serveur (400 ou 401) : déconnexion propre, site gardé
                await _sessionService.ClearSessionAsync();
                return RefreshResult.Rejected(response.StatusCode);
            }

            // Autre code (ex: 5xx ou 429) : on garde la session
            return RefreshResult.NetworkError();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
