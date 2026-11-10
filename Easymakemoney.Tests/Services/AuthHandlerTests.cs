using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Easymakemoney.Models.Session;
using Easymakemoney.Services.Auth;
using Easymakemoney.Tests.Fakes;
using Xunit;

namespace Easymakemoney.Tests.Services;

public class AuthHandlerTests
{
    [Fact]
    public async Task SendAsync_AuthenticatedRoute_AttachesBearerToken()
    {
        var sessionService = new FakeSessionService { Token = "valid_token_123" };
        var notifier = new FakeSessionExpiryNotifier();
        var tokenRefresher = new FakeTokenRefresher(RefreshResult.NetworkError());

        var innerHandler = new TestHttpMessageHandler(req =>
        {
            Assert.Equal("Bearer", req.Headers.Authorization?.Scheme);
            Assert.Equal("valid_token_123", req.Headers.Authorization?.Parameter);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var authHandler = new AuthHandler(sessionService, tokenRefresher, notifier) { InnerHandler = innerHandler };
        var invoker = new HttpMessageInvoker(authHandler);

        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/items");
        var response = await invoker.SendAsync(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(innerHandler.SentRequests);
    }

    [Fact]
    public async Task SendAsync_NoTokenInSession_ThrowsAuthenticationMissingException()
    {
        var sessionService = new FakeSessionService { Token = null };
        var notifier = new FakeSessionExpiryNotifier();
        var tokenRefresher = new FakeTokenRefresher(RefreshResult.NetworkError());

        var innerHandler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var authHandler = new AuthHandler(sessionService, tokenRefresher, notifier) { InnerHandler = innerHandler };
        var invoker = new HttpMessageInvoker(authHandler);

        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/items");

        await Assert.ThrowsAsync<AuthenticationMissingException>(() => invoker.SendAsync(request, CancellationToken.None));
        Assert.Empty(innerHandler.SentRequests);
    }

    [Fact]
    public async Task SendAsync_SkipAuthRoute_DoesNotAttachToken()
    {
        var sessionService = new FakeSessionService { Token = "valid_token_123" };
        var notifier = new FakeSessionExpiryNotifier();
        var tokenRefresher = new FakeTokenRefresher(RefreshResult.NetworkError());

        var innerHandler = new TestHttpMessageHandler(req =>
        {
            Assert.Null(req.Headers.Authorization);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var authHandler = new AuthHandler(sessionService, tokenRefresher, notifier) { InnerHandler = innerHandler };
        var invoker = new HttpMessageInvoker(authHandler);

        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/login");
        request.SetSkipAuth(true);

        var response = await invoker.SendAsync(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SendAsync_SkipAuthRouteReceives401_DoesNotAttemptRefresh()
    {
        var sessionService = new FakeSessionService { Token = null };
        var notifier = new FakeSessionExpiryNotifier();
        var refresherCalled = false;
        var tokenRefresher = new FakeTokenRefresher((_, _) =>
        {
            refresherCalled = true;
            return Task.FromResult(RefreshResult.Success("new_token"));
        });

        var innerHandler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var authHandler = new AuthHandler(sessionService, tokenRefresher, notifier) { InnerHandler = innerHandler };
        var invoker = new HttpMessageInvoker(authHandler);

        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/login");
        request.SetSkipAuth(true);

        var response = await invoker.SendAsync(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(refresherCalled);
        Assert.Equal(0, notifier.NotificationCount);
    }

    [Fact]
    public async Task SendAsync_401AndRefreshRejected_NotifiesAndThrowsSessionExpiredException()
    {
        var sessionService = new FakeSessionService { Token = "expired_token" };
        var notifier = new FakeSessionExpiryNotifier();
        var tokenRefresher = new FakeTokenRefresher(RefreshResult.Rejected(HttpStatusCode.Unauthorized));

        var innerHandler = new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var authHandler = new AuthHandler(sessionService, tokenRefresher, notifier) { InnerHandler = innerHandler };
        var invoker = new HttpMessageInvoker(authHandler);

        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/items");

        await Assert.ThrowsAsync<SessionExpiredException>(() => invoker.SendAsync(request, CancellationToken.None));
        Assert.Equal(1, notifier.NotificationCount);
    }

    [Fact]
    public async Task SendAsync_401AndRefreshSuccess_ReplaysRequestWithNewToken()
    {
        var sessionService = new FakeSessionService { Token = "expired_token" };
        var notifier = new FakeSessionExpiryNotifier();
        var tokenRefresher = new FakeTokenRefresher(RefreshResult.Success("refreshed_token_456"));

        var callCount = 0;
        var innerHandler = new TestHttpMessageHandler(req =>
        {
            callCount++;
            if (callCount == 1)
            {
                Assert.Equal("expired_token", req.Headers.Authorization?.Parameter);
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            Assert.Equal("refreshed_token_456", req.Headers.Authorization?.Parameter);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var authHandler = new AuthHandler(sessionService, tokenRefresher, notifier) { InnerHandler = innerHandler };
        var invoker = new HttpMessageInvoker(authHandler);

        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/items");
        var response = await invoker.SendAsync(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, callCount);
        Assert.Equal(0, notifier.NotificationCount);
    }

    [Fact]
    public async Task SendAsync_401AfterReplay_DoesNotLoopAndReturnsSecond401()
    {
        var sessionService = new FakeSessionService { Token = "token_1" };
        var notifier = new FakeSessionExpiryNotifier();
        var refreshCount = 0;
        var tokenRefresher = new FakeTokenRefresher((_, _) =>
        {
            refreshCount++;
            return Task.FromResult(RefreshResult.Success("token_2"));
        });

        var sendCount = 0;
        var innerHandler = new TestHttpMessageHandler(_ =>
        {
            sendCount++;
            // Même après le rejeu avec token_2, le serveur répond encore 401
            return new HttpResponseMessage(HttpStatusCode.Unauthorized);
        });

        var authHandler = new AuthHandler(sessionService, tokenRefresher, notifier) { InnerHandler = innerHandler };
        var invoker = new HttpMessageInvoker(authHandler);

        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/items");
        var response = await invoker.SendAsync(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(1, refreshCount); // Rafraîchi une seule fois !
        Assert.Equal(2, sendCount);    // Requête initiale + 1 seul rejeu
    }

    [Fact]
    public async Task SendAsync_ReplaysMultipartContent_WithoutDisposalException()
    {
        var sessionService = new FakeSessionService { Token = "old_token" };
        var notifier = new FakeSessionExpiryNotifier();
        var tokenRefresher = new FakeTokenRefresher(RefreshResult.Success("new_token"));

        var callCount = 0;
        string? receivedContentOnReplay = null;

        var innerHandler = new TestHttpMessageHandler(async req =>
        {
            callCount++;
            if (callCount == 1)
            {
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            // Sur le rejeu, lire le contenu multipart pour s'assurer qu'il est intact
            if (req.Content != null)
            {
                receivedContentOnReplay = await req.Content.ReadAsStringAsync();
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var authHandler = new AuthHandler(sessionService, tokenRefresher, notifier) { InnerHandler = innerHandler };
        var invoker = new HttpMessageInvoker(authHandler);

        var multipart = new MultipartFormDataContent();
        multipart.Add(new StringContent("valeur_test"), "champ_texte");
        multipart.Add(new ByteArrayContent(new byte[] { 1, 2, 3, 4, 5 }), "fichier", "test.bin");

        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/upload")
        {
            Content = multipart
        };

        var response = await invoker.SendAsync(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, callCount);
        Assert.NotNull(receivedContentOnReplay);
        Assert.Contains("valeur_test", receivedContentOnReplay);
    }

    [Fact]
    public async Task SendAsync_ThreeConcurrentRequests_TriggerSingleRefreshAndAllSucceed()
    {
        // Règle T7 : concurrence réelle bloquée avec TaskCompletionSource jusqu'à ce que les 3 requêtes reçoivent 401
        var sessionService = new FakeSessionService
        {
            Token = "old_token",
            RefreshToken = "my_refresh_token"
        };
        var notifier = new FakeSessionExpiryNotifier();

        var tcsAllThreeGot401 = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var tcsUnlockRefresh = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var refreshCalls = 0;
        var refreshHandler = new TestHttpMessageHandler(async req =>
        {
            Interlocked.Increment(ref refreshCalls);

            // Attendre le signal que les 3 requêtes ont bien reçu leur 401
            await tcsUnlockRefresh.Task;

            var responseJson = """{ "token": "new_concurrent_token" }""";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
        });

        var authClient = new HttpClient(refreshHandler) { BaseAddress = new Uri("http://localhost/api/") };
        var clientFactory = new FakeHttpClientFactory(_ => authClient);
        var tokenRefresher = new TokenRefresher(clientFactory, sessionService);

        var first401Count = 0;
        var businessHandler = new TestHttpMessageHandler(req =>
        {
            var token = req.Headers.Authorization?.Parameter;
            if (token == "old_token")
            {
                var count = Interlocked.Increment(ref first401Count);
                if (count == 3)
                {
                    // Les 3 requêtes ont reçu leur premier 401 !
                    tcsAllThreeGot401.TrySetResult(true);
                }
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            if (token == "new_concurrent_token")
            {
                return new HttpResponseMessage(HttpStatusCode.OK);
            }

            return new HttpResponseMessage(HttpStatusCode.Forbidden);
        });

        var authHandler = new AuthHandler(sessionService, tokenRefresher, notifier) { InnerHandler = businessHandler };
        var invoker = new HttpMessageInvoker(authHandler);

        // Lancer les 3 requêtes en parallèle
        var task1 = invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/item1"), CancellationToken.None);
        var task2 = invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/item2"), CancellationToken.None);
        var task3 = invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/item3"), CancellationToken.None);

        // Attendre que les 3 requêtes aient reçu leur premier 401
        await tcsAllThreeGot401.Task;

        // Débloquer le rafraîchissement
        tcsUnlockRefresh.SetResult(true);

        var responses = await Task.WhenAll(task1, task2, task3);

        // Les 3 requêtes doivent avoir abouti avec succès (200 OK)
        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));

        // Règle T7 : un seul rafraîchissement réseau exécuté
        Assert.Equal(1, refreshCalls);
        Assert.Equal("new_concurrent_token", sessionService.Token);
    }
}

