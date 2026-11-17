using System.Globalization;
using System.Net;
using Easymakemoney.Models;
using Easymakemoney.Models.Api;
using Easymakemoney.Services;
using Easymakemoney.Services.Api;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.Utilities;
using Easymakemoney.ViewModels.Lists;
using Newtonsoft.Json;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

/// <summary>Prévention des pertes sur le mobile : synthèse, liste, fiche et décision (exemples du backend, tests/Fixtures/pertes).</summary>
[Collection("SiteCurrency")]
public class LossPreventionTests : IDisposable
{
    private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;

    public LossPreventionTests()
    {
        CultureInfo.CurrentCulture = new CultureInfo("fr-CA");
        SiteCurrency.Reset();
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _originalCulture;
        SiteCurrency.Reset();
    }

    private const string SummaryJson = """
    { "period": { "from": "2026-11-14", "to": "2026-11-14" },
      "lastAnalysis": { "at": "2026-11-14T20:28:47+00:00", "pending": false, "blocked": "STAFF_NOTICE_REQUIRED", "skipped": null, "ran": null, "failed": false },
      "counts": { "open": 5, "watch": 0, "new": 2, "active": 5, "byStatus": [], "byRule": [], "byRegister": [] },
      "amountAtStake": 20862, "currency": "CAD",
      "cashDesk": { "closings": 1, "discrepancy": -1200, "discrepancyCount": 1 },
      "staffNotice": { "required": true, "confirmedAt": null },
      "caveats": [
        { "code": "STAFF_NOTICE_REQUIRED", "text": "L'analyse ne démarre qu'après votre confirmation d'avoir informé vos salariés (Réglages)." },
        { "code": "AMOUNT_NOT_A_LOSS", "text": "Le montant en jeu est le montant concerné par le constat, pas une perte prouvée." } ],
      "screen": "pertes.home" }
    """;

    private const string DetailJson = """
    { "id": 6, "number": 6, "rule": { "code": "CASH_DISCREPANCY", "label": "Écart de caisse à la clôture" }, "personPattern": false,
      "status": "open", "statusLabel": "Nouveau",
      "summary": "Clôture n° 1 : il manque 12,00 $ dans la caisse (attendu 132,75 $, compté 120,75 $).",
      "storeDay": "2026-11-14", "cashRegister": { "id": 1, "name": "Caisse principale" }, "amountAtStake": 1200, "currency": "CAD",
      "isNew": false, "newEvidenceSinceDecision": false, "backfill": false,
      "onDuty": [ { "id": 19, "name": "Caisse Nom", "role": "closer", "roleLabel": "Fermeture de la caisse" },
                  { "id": 19, "name": "Caisse Nom", "role": "seller", "roleLabel": "Vente" } ],
      "onDutyNotice": "Ces personnes étaient en poste au moment des faits. Ce n'est pas une accusation : la décision vous revient, après vérification.",
      "amountNotice": "Le montant en jeu est le montant concerné par le constat, pas une perte prouvée.",
      "evidence": [ { "id": 14, "kind": "z_report", "at": "2026-11-14T20:28:47+00:00", "atLabel": "14/11/2026 15 h 28", "title": "Rapport de clôture",
                      "lines": [ { "label": "Espèces attendues", "value": "132,75 $" }, { "label": "Espèces comptées", "value": "120,75 $" } ] } ],
      "evidenceIntact": true, "opinion": null, "decision": null, "history": [], "screen": "pertes.constat" }
    """;

    private static LossSummary Summary() => JsonConvert.DeserializeObject<LossSummary>(SummaryJson)!;
    private static LossFindingDetail Detail() => JsonConvert.DeserializeObject<LossFindingDetail>(DetailJson)!;

    [Fact]
    public void Summary_SaysWhyNothingIsAnalysed_AndKeepsOtherCaveats()
    {
        var summary = Summary();

        Assert.True(summary.IsBlocked);
        Assert.Contains("informé vos salariés", summary.BlockedText);
        Assert.Contains("sur le web", summary.BlockedText);
        Assert.Equal(SiteCurrency.FormatCents(20862, null, "CAD"), summary.AmountText);
        Assert.Equal($"1 clôture de caisse, écart total −{SiteCurrency.FormatCents(1200, null, "CAD")} sur 1 clôture.", summary.CashDeskText);
        Assert.DoesNotContain("Réglages", summary.CaveatsText); // la remarque du blocage n'est pas répétée
        Assert.Contains("pas une perte prouvée", summary.CaveatsText);
    }

    [Fact]
    public void Detail_GroupsRolesOfTheSamePerson_AndNamesNobodyInTheSummary()
    {
        var detail = Detail();

        var person = Assert.Single(detail.OnDutyLines);
        Assert.Equal("Caisse Nom", person.Name);
        Assert.Equal("Fermeture de la caisse, Vente", person.Roles);
        Assert.Equal("Constat 6 · Écart de caisse à la clôture", detail.HeaderText);
        Assert.Contains("Caisse principale", detail.DetailText);
        Assert.Contains("en jeu", detail.DetailText);
        Assert.Equal("Aucune décision pour l'instant.", detail.DecisionText);
        Assert.Equal("14/11/2026 15 h 28", Assert.Single(detail.evidence).WhenText);
    }

    [Fact]
    public void Exercise_ShowsItsNumberWithoutSign()
    {
        var finding = new LossFinding { number = -3, exercise = true };
        Assert.Equal("Exercice 3", finding.NumberText);
        Assert.Equal("Nouveau", new LossFinding { isNew = true }.BadgeText);
        Assert.Equal("Nouvelles preuves", new LossFinding { newEvidenceSinceDecision = true }.BadgeText);
        Assert.Equal("A grossi", new LossFinding { isNew = true, grewSinceDecision = new LossGrowth { before = 4, after = 8 } }.BadgeText);
    }

    [Fact]
    public async Task Service_BuildsTheExpectedRoutes()
    {
        var http = new FakeHttpService { ResultToReturn = new List<LossFinding>() };
        var service = new LossPreventionService(http);

        await service.GetFindingsAsync("open,watch", false, 1, 20);
        await service.GetFindingsAsync(null, true, 2, 20);

        Assert.Equal(new[]
        {
            "gestion/pertes/constats?status=open%2Cwatch&sort=date&page=1&limit=20&locale=fr",
            "gestion/pertes/constats?isNew=1&sort=date&page=2&limit=20&locale=fr",
        }, http.RequestedUrls);
    }

    [Fact]
    public async Task List_LoadsSummaryThenActiveFindings_AndFilterNewAsksIsNew()
    {
        var service = new FakeLossPreventionService { Summary = Summary(), Findings = { new LossFinding { id = 6 } } };
        var vm = new LossFindingListViewModel(service, new FakeDialogService());

        await vm.InitializeAsync();
        Assert.True(vm.HasSummary);
        Assert.Single(vm.Findings);
        Assert.Equal(("open,watch", false), service.Queries.Last());

        await vm.SelectFilterCommand.ExecuteAsync(LossFindingListViewModel.FilterNew);
        Assert.Equal((null, true), service.Queries.Last());
    }

    [Fact]
    public async Task List_InitialFilterFromTheTile_OpensNewFindings()
    {
        var service = new FakeLossPreventionService { Summary = Summary() };
        var vm = new LossFindingListViewModel(service, new FakeDialogService());

        vm.UseInitialFilter("new");
        await vm.InitializeAsync();

        Assert.Equal((null, true), service.Queries.Single());
        Assert.True(vm.IsEmpty);
    }

    [Fact]
    public async Task List_ModuleNotGranted_ShowsTheCard_WithoutClosingTheApp()
    {
        var service = new FakeLossPreventionService
        {
            SummaryException = new ApiException(new ApiError { Code = ModuleInactiveInfo.Code, Message = "Module non activé." }, HttpStatusCode.Forbidden),
        };
        var dialog = new FakeDialogService();
        var vm = new LossFindingListViewModel(service, dialog);

        await vm.InitializeAsync();

        Assert.True(vm.IsModuleInactive);
        Assert.Empty(service.Queries); // la liste n'est pas demandée
        Assert.Empty(dialog.ShownErrors);
    }

    [Fact]
    public async Task Decision_RequiresANoteForExplainedAndConfirmed()
    {
        var service = new FakeLossPreventionService { Detail = Detail() };
        var dialog = new FakeDialogService();
        var vm = new LossFindingViewModel(service, dialog) { FindingId = 6 };
        await vm.LoadAsync();
        LossFindingListViewModel.FindingsChanged = false;

        vm.SelectDecisionCommand.Execute("confirmed");
        Assert.True(vm.CanDecide);
        await vm.DecideCommand.ExecuteAsync(null);
        Assert.Empty(service.Decisions);
        Assert.Single(dialog.ShownInfos);

        vm.Note = "Recompté avec le responsable : erreur de monnaie confirmée.";
        await vm.DecideCommand.ExecuteAsync(null);
        Assert.Equal((6, "confirmed", "Recompté avec le responsable : erreur de monnaie confirmée."), service.Decisions.Single());
        Assert.True(LossFindingListViewModel.FindingsChanged);
        Assert.Equal(string.Empty, vm.Note);
        Assert.Null(vm.SelectedDecision);
    }

    [Fact]
    public async Task Decision_WatchNeedsNoNote_AndTheSameStatusCannotBeResent()
    {
        var service = new FakeLossPreventionService { Detail = Detail() };
        var vm = new LossFindingViewModel(service, new FakeDialogService()) { FindingId = 6 };
        await vm.LoadAsync();

        vm.SelectDecisionCommand.Execute("open"); // déjà « Nouveau »
        Assert.False(vm.CanDecide);

        vm.SelectDecisionCommand.Execute("watch");
        await vm.DecideCommand.ExecuteAsync(null);
        Assert.Equal((6, "watch", ""), service.Decisions.Single());
    }

    private sealed class FakeLossPreventionService : ILossPreventionService
    {
        public LossSummary Summary { get; set; } = new();
        public Exception? SummaryException { get; set; }
        public List<LossFinding> Findings { get; } = new();
        public LossFindingDetail Detail { get; set; } = new();
        public List<(string? Status, bool OnlyNew)> Queries { get; } = new();
        public List<(int Id, string Status, string? Note)> Decisions { get; } = new();

        public Task<LossSummary> GetSummaryAsync(bool analyse = true) =>
            SummaryException != null ? Task.FromException<LossSummary>(SummaryException) : Task.FromResult(Summary);

        public Task<List<LossFinding>> GetFindingsAsync(string? status, bool onlyNew, int page, int limit)
        {
            Queries.Add((status, onlyNew));
            return Task.FromResult(Findings.ToList());
        }

        public Task<LossFindingDetail> GetFindingAsync(int id) => Task.FromResult(Detail);

        public Task<LossFindingDetail> DecideAsync(int id, string status, string? note)
        {
            Decisions.Add((id, status, note));
            Detail.status = status;
            return Task.FromResult(Detail);
        }

        public Task<LossExercises> GetExercisesAsync() => Task.FromResult(new LossExercises());
        public Task<LossExerciseLaunch> LaunchExerciseAsync(string scenario, bool sendTestEmail) => Task.FromResult(new LossExerciseLaunch());
        public Task<LossFindingDetail> GetExerciseAsync(int id) => Task.FromResult(Detail);
        public Task<LossFindingDetail> DecideExerciseAsync(int id, string status, string? note) => DecideAsync(id, status, note);
        public Task DeleteExercisesAsync() => Task.CompletedTask;
        public Task<LossHealth> GetHealthAsync() => Task.FromResult(new LossHealth());
        public Task<LossBoxList> GetBoxesAsync() => Task.FromResult(new LossBoxList());
        public Task<LossBoxPairResult> PairBoxAsync(string code, string name, string? place) => Task.FromResult(new LossBoxPairResult());
    }
}

/// <summary>Mode exercice (EX-1) sur le mobile : routes à part, aperçu de l'alerte, effacement confirmé.</summary>
[Collection("SiteCurrency")]
public class LossExerciseTests
{
    [Fact]
    public async Task Service_UsesTheExerciseRoutesOnly()
    {
        var http = new FakeHttpService { ResultToReturn = new LossExercises() };
        var service = new LossPreventionService(http);

        await service.GetExercisesAsync();
        http.ResultToReturn = new LossFindingDetail();
        await service.GetExerciseAsync(12);

        Assert.Equal(new[] { "gestion/pertes/exercices?locale=fr", "gestion/pertes/exercices/12?locale=fr" }, http.RequestedUrls);
    }

    [Fact]
    public void Launch_PreviewSaysWhatTheAlertWouldSay_AndTheTestEmailFate()
    {
        var launch = JsonConvert.DeserializeObject<LossExerciseLaunch>("""
        { "finding": { "id": 12, "number": -1, "exercise": true },
          "alertPreview": { "tile": { "title": "Prévention des pertes", "text": "1 nouveau constat à vérifier, 10,00 $ en jeu." },
                            "email": { "subject": "Prévention des pertes : 1 nouveau constat à vérifier", "message": "…" } },
          "testEmail": { "requested": true, "sent": false, "reason": "DAILY_LIMIT_REACHED" }, "remaining": 29 }
        """)!;

        Assert.Contains("« 1 nouveau constat à vérifier, 10,00 $ en jeu. »", launch.PreviewText);
        Assert.Contains("limite du jour atteinte", launch.PreviewText);
        Assert.Equal("Exercice 1", launch.finding!.NumberText);
    }

    [Fact]
    public void Limits_SayWhatIsLeft()
    {
        var data = new LossExercises { limit = 30, remaining = 29, daily = new LossDaily { limit = 30, remaining = 0 } };
        Assert.Equal("29 exercices encore possibles sur 30 · lancements du jour épuisés", data.LimitsText);
        Assert.False(data.CanLaunch);
        Assert.True(new LossExercises { limit = 30, remaining = 30, daily = new LossDaily { limit = 1000000, remaining = 1000000 } }.CanLaunch);
    }

    [Fact]
    public async Task ExerciseDetail_ReadsAndDecidesThroughExerciseRoutes()
    {
        var http = new FakeHttpService { ResultToReturn = new LossFindingDetail { id = 12, number = -1, exercise = true, status = "open" } };
        var vm = new LossFindingViewModel(new LossPreventionService(http), new FakeDialogService()) { FindingId = 12, IsExercise = true };

        await vm.LoadAsync();
        vm.SelectDecisionCommand.Execute("watch");
        await vm.DecideCommand.ExecuteAsync(null);

        Assert.Equal("gestion/pertes/exercices/12?locale=fr", http.RequestedUrls[0]);
        Assert.DoesNotContain(http.RequestedUrls, url => url.Contains("/constats/"));
    }

    [Fact]
    public void Constat_qui_a_grossi_lu_depuis_le_json()
    {
        var json = "{\"id\":5,\"number\":12,\"isNew\":true,\"grewSinceDecision\":{\"at\":\"2026-11-17T10:00:00+00:00\",\"before\":4,\"after\":8},"
            + "\"history\":[{\"kind\":\"grew\",\"at\":\"2026-11-17T10:00:00+00:00\",\"by\":null,\"before\":4,\"after\":8,\"statusLabel\":\"A grossi depuis votre décision : 4 puis 8\"},"
            + "{\"kind\":\"decision\",\"at\":\"2026-11-10T10:00:00+00:00\",\"status\":\"explained\",\"statusLabel\":\"Expliqué\",\"by\":{\"id\":1,\"name\":\"Gérant\"}}]}";
        var finding = System.Text.Json.JsonSerializer.Deserialize<LossFindingDetail>(json)!;

        Assert.True(finding.HasGrown);
        Assert.Equal("A grossi depuis votre décision : 4 à la décision, 8 maintenant.", finding.GrewText);
        Assert.Equal("A grossi", finding.BadgeText);
        Assert.EndsWith("A grossi depuis votre décision : 4 puis 8", finding.history[0].Text);
        Assert.DoesNotContain("—", finding.history[0].Text);
        Assert.EndsWith("Expliqué · Gérant", finding.history[1].Text);
    }

    [Fact]
    public void Constat_sans_croissance()
    {
        var finding = System.Text.Json.JsonSerializer.Deserialize<LossFinding>("{\"id\":5,\"number\":12,\"grewSinceDecision\":null}")!;
        Assert.False(finding.HasGrown);
        Assert.Equal(string.Empty, finding.GrewText);
    }
}
