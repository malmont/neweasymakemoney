using System.Net;
using System.Text;
using Easymakemoney.Models.Session;
using Easymakemoney.Services.Offline;
using Easymakemoney.Tests.Fakes;
using Xunit;

namespace Easymakemoney.Tests.Services;

/// <summary>Hors connexion : statistiques et trésorerie servies depuis la dernière copie, datée, propre au site et au compte.</summary>
[Collection("SiteCurrency")]
public class OfflineCacheTests
{
    private sealed class MemoryCache : IResponseCache
    {
        public Dictionary<string, (string Body, DateTime SavedAt)> Items { get; } = new();
        public void Save(string key, string body, DateTime savedAt) => Items[key] = (body, savedAt);
        public (string Body, DateTime SavedAt)? Load(string key) => Items.TryGetValue(key, out var item) ? item : null;
        public void Clear() => Items.Clear();
    }

    private static HttpRequestMessage Get(string url, string site = "demo.example")
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/" + url);
        request.Headers.Add("X-Tenant-Host", site);
        return request;
    }

    private static (HttpMessageInvoker Invoker, MemoryCache Cache, FakeSessionService Session) Build(Func<HttpRequestMessage, HttpResponseMessage> server, MemoryCache? cache = null, int userId = 7)
    {
        cache ??= new MemoryCache();
        var session = new FakeSessionService { CurrentUser = new UserInfo { Id = userId } };
        var handler = new OfflineCacheHandler(cache, session) { InnerHandler = new TestHttpMessageHandler(server) };
        return (new HttpMessageInvoker(handler), cache, session);
    }

    private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static HttpResponseMessage NoNetwork(HttpRequestMessage _) => throw new HttpRequestException("réseau indisponible");

    [Fact]
    public async Task Statistiques_SansReseau_ServentLaDerniereCopie_EtAnnoncentHorsConnexion()
    {
        var online = true;
        var cache = new MemoryCache();
        var (invoker, _, _) = Build(req => online ? Json("{\"total\":1200}") : NoNetwork(req), cache);

        var first = await invoker.SendAsync(Get("gestion/statistiques?range=today"), CancellationToken.None);
        Assert.Equal("{\"total\":1200}", await first.Content.ReadAsStringAsync());
        Assert.Null(OfflineState.ServedAt);
        Assert.Single(cache.Items);

        online = false;
        var offline = await invoker.SendAsync(Get("gestion/statistiques?range=today"), CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, offline.StatusCode);
        Assert.Equal("{\"total\":1200}", await offline.Content.ReadAsStringAsync());
        Assert.NotNull(OfflineState.ServedAt);

        online = true;
        await invoker.SendAsync(Get("gestion/tresorerie?days=14"), CancellationToken.None);
        Assert.Null(OfflineState.ServedAt);
    }

    [Fact]
    public async Task SansCopie_LErreurReseauRemonte()
    {
        var (invoker, _, _) = Build(NoNetwork);

        await Assert.ThrowsAsync<HttpRequestException>(() => invoker.SendAsync(Get("gestion/tresorerie?days=14"), CancellationToken.None));
    }

    [Fact]
    public async Task Copie_PropreAuSiteEtAuCompte()
    {
        var cache = new MemoryCache();
        var (invoker, _, _) = Build(_ => Json("{\"a\":1}"), cache);
        await invoker.SendAsync(Get("gestion/tresorerie"), CancellationToken.None);

        var (offline, _, _) = Build(NoNetwork, cache, userId: 8);
        await Assert.ThrowsAsync<HttpRequestException>(() => offline.SendAsync(Get("gestion/tresorerie"), CancellationToken.None));

        var (otherSite, _, _) = Build(NoNetwork, cache, userId: 7);
        await Assert.ThrowsAsync<HttpRequestException>(() => otherSite.SendAsync(Get("gestion/tresorerie", "autre.example"), CancellationToken.None));

        var sameAccount = await otherSite.SendAsync(Get("gestion/tresorerie"), CancellationToken.None);
        Assert.Equal("{\"a\":1}", await sameAccount.Content.ReadAsStringAsync());
        OfflineState.Online();
    }

    [Fact]
    public async Task SansCompteOuSansSite_RienNEstGarde()
    {
        var cache = new MemoryCache();
        var session = new FakeSessionService { CurrentUser = null };
        var handler = new OfflineCacheHandler(cache, session) { InnerHandler = new TestHttpMessageHandler(_ => Json("{}")) };
        var invoker = new HttpMessageInvoker(handler);

        await invoker.SendAsync(Get("gestion/statistiques"), CancellationToken.None);
        var noSite = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/gestion/statistiques");
        await invoker.SendAsync(noSite, CancellationToken.None);

        Assert.Empty(cache.Items);
    }

    [Theory]
    [InlineData("GET", "gestion/statistiques?range=today", true)]
    [InlineData("GET", "gestion/statistiques/annee?year=2026", true)]
    [InlineData("GET", "gestion/tresorerie/soldes?limit=5", true)]
    [InlineData("GET", "gestion/locations/statistiques", true)]
    [InlineData("POST", "gestion/tresorerie/soldes", false)]
    [InlineData("GET", "gestion/journee?locale=fr", false)]
    [InlineData("GET", "gestion/prix/lots?limit=5", false)]
    [InlineData("GET", "products", false)]
    public void SeulesLesLecturesDesStatistiquesEtDeLaTresorerieSontGardees(string method, string url, bool expected)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), "http://localhost/api/" + url);
        Assert.Equal(expected, OfflineCacheHandler.IsCacheable(request));
    }

    [Fact]
    public async Task UneEcritureNEstJamaisGardeeNiServie()
    {
        var (invoker, cache, _) = Build(_ => Json("{\"ok\":true}"));
        var post = new HttpRequestMessage(HttpMethod.Post, "http://localhost/api/gestion/tresorerie/soldes");
        post.Headers.Add("X-Tenant-Host", "demo.example");

        await invoker.SendAsync(post, CancellationToken.None);

        Assert.Empty(cache.Items);
    }

    [Fact]
    public async Task UneReponseEnErreurNEstPasGardee()
    {
        var (invoker, cache, _) = Build(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("{}") });

        await invoker.SendAsync(Get("gestion/statistiques"), CancellationToken.None);

        Assert.Empty(cache.Items);
    }

    [Fact]
    public void Bandeau_DateDuJourOuDUnAutreJour()
    {
        var now = new DateTime(2026, 11, 12, 18, 0, 0);
        Assert.Equal("Hors connexion : données de 14 h 05", OfflineState.Text(new DateTime(2026, 11, 12, 14, 5, 0), now));
        Assert.Equal("Hors connexion : données du 10 novembre à 9 h 30", OfflineState.Text(new DateTime(2026, 11, 10, 9, 30, 0), now));
        Assert.Equal(string.Empty, OfflineState.Text(null, now));
    }
}
