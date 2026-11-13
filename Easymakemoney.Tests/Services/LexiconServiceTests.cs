using Easymakemoney.Components;
using Easymakemoney.Services;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.Utilities;
using Xunit;

namespace Easymakemoney.Tests.Services;

/// <summary>W2-APPRENDRE : le lexique du serveur passe avant le glossaire local, qui reste le secours.</summary>
[Collection("Glossaire")]
public class LexiconServiceTests : IDisposable
{
    public LexiconServiceTests() => Glossaire.ClearServer();
    public void Dispose() => Glossaire.ClearServer();

    private static LexiconResponse Lexicon(string version, string definition) => new()
    {
        version = version,
        entries =
        {
            new LexiconEntry { key = "marge", term = "Marge brute", definition = definition },
            new LexiconEntry { key = "nouvelle-notion", term = "Nouvelle notion", definition = "Définie par le serveur." },
            new LexiconEntry { key = "vide", term = "", definition = "" },
        },
    };

    [Fact]
    public async Task Refresh_ServerDefinitionsWin_LocalRemainsFallback()
    {
        var http = new FakeHttpService { ResultToReturn = Lexicon("v1", "Définition du serveur.") };
        var service = new LexiconService(http);

        await service.RefreshAsync();

        Assert.Equal("gestion/lexique?locale=fr", Assert.Single(http.RequestedUrls));
        Assert.Equal("v1", Glossaire.ServerVersion);
        Assert.Equal("Définition du serveur.", Glossaire.Find("marge")!.Definition);
        Assert.Equal("Nouvelle notion", Glossaire.Find("nouvelle-notion")!.Title);
        Assert.Null(Glossaire.Find("vide"));
        // Une notion absente du serveur garde sa définition locale.
        Assert.NotNull(Glossaire.Find("arrondi"));
    }

    [Fact]
    public async Task Refresh_AtMostEveryThirtyMinutes_AndRetriesAfterAnError()
    {
        var now = new DateTime(2026, 11, 13, 22, 0, 0, DateTimeKind.Utc);
        var http = new FakeHttpService { ResultToReturn = Lexicon("v1", "A.") };
        var service = new LexiconService(http, () => now);

        await service.RefreshAsync();
        now = now.AddMinutes(10);
        await service.RefreshAsync();
        Assert.Single(http.RequestedUrls);

        now = now.AddMinutes(25);
        http.ResultToReturn = Lexicon("v2", "B.");
        await service.RefreshAsync();
        Assert.Equal(2, http.RequestedUrls.Count);
        Assert.Equal("B.", Glossaire.Find("marge")!.Definition);
    }

    [Fact]
    public void ClearServer_FallsBackToLocalGlossary()
    {
        Glossaire.ApplyServer("v1", new[] { ("marge", (string?)"Marge brute", (string?)"Serveur.") });
        Glossaire.ClearServer();

        Assert.Null(Glossaire.ServerVersion);
        Assert.StartsWith("Ventes moins le coût d'achat", Glossaire.Find("marge")!.Definition);
    }

    [Fact]
    public void TermDefinitionText_AddsIllustrationOnlyWhenPresent()
    {
        Assert.Equal("Déf.", TermDefinition.Text("Déf.", null));
        Assert.Equal("Déf.", TermDefinition.Text("Déf.", "  "));
        Assert.Equal("Déf.\n\nChez vous : Votre taux de marque ce mois-ci : 44 %.", TermDefinition.Text("Déf.", "Votre taux de marque ce mois-ci : 44 %."));
    }

    [Fact]
    public async Task Illustration_ReadsTheSiteSentence_OrNull()
    {
        var http = new FakeHttpService { ResultToReturn = new LexiconEntry { key = "taux-de-marque", site = new LexiconSite { illustration = "Votre taux de marque ce mois-ci : 44 %." } } };
        var service = new LexiconService(http);

        Assert.Equal("Votre taux de marque ce mois-ci : 44 %.", await service.GetIllustrationAsync("taux-de-marque"));
        Assert.Equal("gestion/lexique/taux-de-marque?locale=fr", Assert.Single(http.RequestedUrls));

        http.ResultToReturn = new LexiconEntry { key = "arrondi", site = null };
        Assert.Null(await service.GetIllustrationAsync("arrondi"));
    }
}
