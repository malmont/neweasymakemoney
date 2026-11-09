using System.Net;
using System.Net.Http.Headers;
using Easymakemoney.Services.Session;

namespace Easymakemoney.Services.Auth;

public class AuthHandler : DelegatingHandler
{
    private readonly ISessionService _sessionService;
    private readonly ITokenRefresher _tokenRefresher;
    private readonly ISessionExpiryNotifier _sessionExpiryNotifier;

    public AuthHandler(
        ISessionService sessionService,
        ITokenRefresher tokenRefresher,
        ISessionExpiryNotifier sessionExpiryNotifier)
    {
        _sessionService = sessionService;
        _tokenRefresher = tokenRefresher;
        _sessionExpiryNotifier = sessionExpiryNotifier;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Si la requête est explicitement marquée comme publique (skip auth)
        if (request.GetSkipAuth())
        {
            return await base.SendAsync(request, cancellationToken);
        }

        // Requête authentifiée : obtenir le jeton de la session
        var token = await _sessionService.GetTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new AuthenticationMissingException();
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Règle N4 : bufferiser le corps avant le premier envoi pour pouvoir le rejouer (ex: multipart avec image)
        if (request.Content != null)
        {
            await request.Content.LoadIntoBufferAsync();
        }

        var response = await base.SendAsync(request, cancellationToken);

        // Si la réponse n'est pas 401, on la retourne directement
        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        // 401 Unauthorized sur une requête authentifiée : tentative de rafraîchissement
        var refreshResult = await _tokenRefresher.RefreshAsync(token, cancellationToken);

        if (refreshResult.IsSuccess && !string.IsNullOrWhiteSpace(refreshResult.NewToken))
        {
            // Libérer l'ancienne réponse 401
            response.Dispose();

            // Cloner la requête initiale pour la rejouer une seule fois avec le nouveau jeton
            var clonedRequest = await CloneHttpRequestMessageAsync(request);
            clonedRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshResult.NewToken);

            return await base.SendAsync(clonedRequest, cancellationToken);
        }

        if (refreshResult.Status == RefreshStatus.Rejected)
        {
            // Le serveur a refusé le rafraîchissement (400 ou 401)
            // Règle N3 : AuthHandler appelle le notificateur sans l'attendre, puis lève SessionExpiredException
            _ = _sessionExpiryNotifier.NotifySessionExpiredAsync();
            throw new SessionExpiredException();
        }

        // Serveur injoignable (NetworkError) : la session est conservée, la réponse 401 d'origine remonte
        return response;
    }

    private static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version
        };

        // Copier les options de requête
        foreach (var option in request.Options)
        {
            clone.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);
        }

        // Copier les en-têtes de requête
        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Copier le corps si présent
        if (request.Content != null)
        {
            var ms = new MemoryStream();
            await request.Content.CopyToAsync(ms);
            ms.Position = 0;

            clone.Content = new StreamContent(ms);

            foreach (var header in request.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return clone;
    }
}
