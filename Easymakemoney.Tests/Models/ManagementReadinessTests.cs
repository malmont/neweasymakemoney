using Easymakemoney.Models;
using Easymakemoney.Services;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.ViewModels.Lists;
using Newtonsoft.Json;
using Xunit;

namespace Easymakemoney.Tests.Models;

/// <summary>Lot CHE : « Préparer ma gestion », groupe app.management de GET /api/site/readiness.</summary>
public class ManagementReadinessTests
{
    // Forme réelle de /api/site/readiness (SiteReadinessService) avec les ajouts du contrat CHE (appScreen, figures).
    private const string Json = """
    { "code": "demo", "percent": 60, "done": 3, "total": 5, "next": null, "blockingTodo": 1, "improvementTodo": 2,
      "steps": [
        { "key": "site.identity", "group": "site", "title": "Identité du site", "description": "x", "status": "done", "where": "backend", "action": null, "blocking": true },
        { "key": "management.key", "group": "app.management", "title": "Clé de l'application de gestion", "description": "Générez la clé.", "status": "done", "where": "backend", "blocking": true, "appScreen": null },
        { "key": "management.fiscal_year", "group": "app.management", "title": "Début de l'exercice", "description": "Choisissez le mois.", "status": "todo", "where": "backend", "blocking": true, "appScreen": "exercice" },
        { "key": "management.short_code", "group": "app.management", "title": "Code court", "description": "y", "status": "todo", "where": "backend", "blocking": true, "appScreen": null },
        { "key": "management.purchase_cost", "group": "app.management", "title": "Coût d'achat des articles", "description": "12 articles sur 40 sans coût d'achat.", "status": "todo", "where": "backend", "blocking": false, "appScreen": "articles", "figures": { "missing": 12, "total": 40 } },
        { "key": "management.device", "group": "app.management", "title": "Appareil pour les notifications", "description": "z", "status": "todo", "blocking": false, "appScreen": "notifications" },
        { "key": "management.fixed_costs", "group": "app.management", "title": "Frais fixes", "description": "w", "status": "done", "blocking": false, "appScreen": "frais-fixes" } ],
      "groups": [ { "key": "app.management", "label": "Application de gestion" } ], "links": null }
    """;

    private static SiteReadiness Parse() => JsonConvert.DeserializeObject<SiteReadiness>(Json)!;

    [Fact]
    public void ManagementSteps_KeepOnlyItsGroup()
    {
        var steps = Parse().ManagementSteps;

        Assert.Equal(6, steps.Count);
        Assert.DoesNotContain(steps, step => step.key == "site.identity");
    }

    [Fact]
    public void Summary_SortsBlockingImprovementsDone()
    {
        var summary = new ManagementReadinessSummary(Parse().ManagementSteps);

        Assert.Equal(new[] { "management.fiscal_year", "management.short_code" }, summary.Blocking.Select(step => step.key));
        Assert.Equal(new[] { "management.purchase_cost", "management.device" }, summary.Improvements.Select(step => step.key));
        Assert.Equal(new[] { "management.key", "management.fixed_costs" }, summary.Done.Select(step => step.key));
        Assert.Equal(33, summary.Percent);
        Assert.False(summary.IsReady);
        Assert.StartsWith("2 étapes à faire", summary.HeadlineText);
        Assert.Equal("2 sur 6 · 33 %", summary.ProgressText);
    }

    [Fact]
    public void Summary_OnlyImprovementsLeft_SaysEssentialReady()
    {
        var steps = new List<ReadinessStep>
        {
            new() { key = "a", status = "done", blocking = true },
            new() { key = "b", status = "todo", blocking = false },
        };

        var summary = new ManagementReadinessSummary(steps);

        Assert.True(summary.IsReady);
        Assert.StartsWith("L'essentiel est prêt. 1 réglage conseillé", summary.HeadlineText);
    }

    [Fact]
    public void Summary_Empty_IsNeutral()
    {
        var summary = new ManagementReadinessSummary(new List<ReadinessStep>());

        Assert.Equal(100, summary.Percent);
        Assert.Equal("Aucune étape à afficher pour l'instant.", summary.HeadlineText);
    }

    [Theory]
    [InlineData("periodes", "ListNewCollectionPage")]
    [InlineData("exercice", "FiscalYearPage")]
    [InlineData("articles", "AttachProductsPage")]
    [InlineData("fournisseurs", "ListNewFournisseurPage")]
    [InlineData("tresorerie", "TreasuryPage")]
    [InlineData("frais-fixes", "FixedCostsPage")]
    [InlineData("commandes", "ListNewCollectionPage")]
    [InlineData("notifications", null)]
    [InlineData(null, null)]
    [InlineData("inconnu", null)]
    public void Routes_FollowContractCodes(string? appScreen, string? route)
    {
        Assert.Equal(route, ReadinessRoutes.For(appScreen));
    }

    [Fact]
    public void Step_TextsAndActions()
    {
        var steps = Parse().ManagementSteps.ToDictionary(step => step.key);

        Assert.True(steps["management.fiscal_year"].CanOpen);
        Assert.Equal("À faire", steps["management.fiscal_year"].StateText);
        Assert.True(steps["management.short_code"].IsOnSite);
        Assert.Equal("À faire sur le site (administration)", steps["management.short_code"].ActionText);
        Assert.Equal("Bientôt dans l'application", steps["management.device"].ActionText);
        Assert.Equal("Conseillé", steps["management.purchase_cost"].StateText);
        Assert.Equal(12, steps["management.purchase_cost"].figures!.Value<int>("missing"));
        // Étape faite : ni lien ni mention « sur le site »
        Assert.False(steps["management.key"].CanOpen);
        Assert.False(steps["management.key"].IsOnSite);
    }

    [Fact]
    public void Step_PrefersContractLabelAndHint()
    {
        var step = new ReadinessStep { key = "k", title = "Titre", description = "Description", label = "Libellé", hint = "Ce que ça débloque" };

        Assert.Equal("Libellé", step.Title);
        Assert.Equal("Ce que ça débloque", step.Text);
    }

    [Fact]
    public void Step_OptionalNeverBlocks()
    {
        var step = new ReadinessStep { key = "k", status = "optional", blocking = true };

        Assert.False(step.IsBlockingTodo);
        Assert.True(step.IsImprovementTodo);
    }

    [Fact]
    public async Task Service_ReadsSiteReadiness()
    {
        var http = new FakeHttpService { ResultToReturn = new SiteReadiness() };

        await new ManagementModuleService(http).GetReadinessAsync();

        Assert.Equal("site/readiness", http.RequestedUrls.Single());
    }

    [Fact]
    public async Task ViewModel_LoadsSectionsAndOpensScreen()
    {
        var service = new ManagementModuleTests.FakeManagementModuleService { Readiness = Parse() };
        var dialogs = new FakeDialogService();
        var opened = new List<string>();
        var vm = new ManagementReadinessViewModel(service, dialogs) { NavigateAsync = route => { opened.Add(route); return Task.CompletedTask; } };

        await vm.LoadAsync();

        Assert.True(vm.IsLoaded);
        Assert.Equal(2, vm.Blocking.Count);
        Assert.Equal(2, vm.Improvements.Count);
        Assert.Equal(2, vm.Done.Count);

        await vm.OpenCommand.ExecuteAsync(vm.Blocking.First(step => step.key == "management.fiscal_year"));
        Assert.Equal("FiscalYearPage", opened.Single());

        // Étape sans écran : un message, pas de navigation
        await vm.OpenCommand.ExecuteAsync(vm.Blocking.First(step => step.key == "management.short_code"));
        Assert.Single(opened);
        Assert.Contains("sur le site", dialogs.ShownInfos.Single());

        // Étape faite : rien
        await vm.OpenCommand.ExecuteAsync(vm.Done.First());
        Assert.Single(opened);
        Assert.Single(dialogs.ShownInfos);
    }

    [Fact]
    public async Task ViewModel_Error_ShowsErrorOnce()
    {
        var service = new ManagementModuleTests.FakeManagementModuleService { Error = new InvalidOperationException("x") };
        var dialogs = new FakeDialogService();
        var vm = new ManagementReadinessViewModel(service, dialogs);

        await vm.LoadAsync();

        Assert.False(vm.IsLoaded);
        Assert.Single(dialogs.ShownErrors);
    }
}
