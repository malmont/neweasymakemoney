using System.Globalization;
using System.Net;
using Easymakemoney.Models;
using Easymakemoney.Models.Api;
using Easymakemoney.Services.Api;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.Utilities;
using Easymakemoney.ViewModels.Lists;
using Newtonsoft.Json;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

[Collection("SiteCurrency")]
public class PeriodSimulationViewModelTests : IDisposable
{
    private readonly CultureInfo _originalCulture;
    private readonly FakeSimulationService _simulationService = new();
    private readonly FakePeriodBudgetService _budgetService = new();
    private readonly FakeDialogService _dialogService = new();

    public PeriodSimulationViewModelTests()
    {
        _originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        SiteCurrency.Reset();
        SiteCurrency.SetCurrency("CAD");
    }

    public void Dispose()
    {
        SiteCurrency.Reset();
        CultureInfo.CurrentCulture = _originalCulture;
    }

    private PeriodSimulationViewModel CreateViewModel()
    {
        return new PeriodSimulationViewModel(_simulationService, _budgetService, _dialogService)
        {
            PeriodId = 7
        };
    }

    private static PeriodSimulation SampleSimulation()
    {
        const string json = """
        {
          "period": { "id": 7, "name": "Automne 2026", "daysTotal": 90, "daysElapsed": 30, "daysRemaining": 60 },
          "currency": "CAD",
          "base": {
            "sales": { "realized": 100000, "projected": 50000, "total": 150000 },
            "grossMargin": 60000,
            "result": 10000
          },
          "simulated": {
            "sales": { "realized": 100000, "projected": 55000, "total": 155000 },
            "grossMargin": 62000,
            "result": 12000
          },
          "plannedToSave": {
            "lines": [ { "category": "advertising", "planned": 10000 } ]
          }
        }
        """;
        return JsonConvert.DeserializeObject<PeriodSimulation>(json)!;
    }

    private static ApiException MakeApiException(string code, HttpStatusCode statusCode, string message = "Erreur")
    {
        var apiError = ApiError.FromHttpResponse($$"""{ "error": "{{code}}", "code": "{{code}}", "message": "{{message}}" }""", statusCode, null);
        return new ApiException(apiError, statusCode);
    }

    [Fact]
    public async Task Simulation_Start_WhenSimulationNull_CallsSimulateAsync()
    {
        var vm = CreateViewModel();
        _simulationService.SimulationResult = SampleSimulation();

        await vm.StartAsync();

        var call = Assert.Single(_simulationService.SimulateCalls);
        Assert.Equal(7, call.PeriodId);
        Assert.NotNull(vm.Simulation);
        Assert.True(vm.HasSimulation);
        Assert.NotEmpty(vm.Rows);
        Assert.False(vm.CanSaveAdvertising); // Pas de publicité saisie
    }

    [Fact]
    public async Task Simulation_Reset_ClearsParametersAndLevers()
    {
        var vm = CreateViewModel();
        _simulationService.SimulationResult = SampleSimulation();
        await vm.StartAsync();

        vm.AdvertisingText = "100";
        vm.PricePercent = 5;
        vm.PromotionPercent = 10;

        vm.ResetCommand.Execute(null);

        Assert.Null(vm.AdvertisingText);
        Assert.Equal(0, vm.PricePercent);
        Assert.Equal(0, vm.PromotionPercent);

        await vm.RunSimulationAsync();

        var lastCall = _simulationService.SimulateCalls.Last();
        Assert.Null(lastCall.Levers.pricePercent);
        Assert.Null(lastCall.Levers.promotion);
        Assert.Null(lastCall.Levers.advertisingDelta);
    }

    [Fact]
    public async Task Simulation_PresetPromotion_SetsPercentAndRestrictsToProducts()
    {
        var vm = CreateViewModel();
        _simulationService.SimulationResult = SampleSimulation();
        await vm.StartAsync();

        vm.PresetPromotion(20, new[] { 101, 102 });

        Assert.Equal(20, vm.PromotionPercent);
        Assert.Contains("2 articles choisis", vm.PromotionText);

        await vm.RunSimulationAsync();

        var lastCall = _simulationService.SimulateCalls.Last();
        Assert.NotNull(lastCall.Levers.promotion);
        Assert.Equal(20, lastCall.Levers.promotion.percent);
        Assert.Equal("products", lastCall.Levers.promotion.scope);
        Assert.Equal(new[] { 101, 102 }, lastCall.Levers.promotion.productIds);
    }

    [Fact]
    public async Task Simulation_AskGuide_ExplicitAction_CallsCommentAsync()
    {
        var vm = CreateViewModel();
        _simulationService.SimulationResult = SampleSimulation();
        _simulationService.CommentResult = new SimulationComment
        {
            candidates = new List<SimulationCandidate>
            {
                new() { key = "keep", label = "Garder le prévu", IsRecommended = false },
                new() { key = "safe", label = "Trésorerie sûre", IsRecommended = true }
            }
        };

        await vm.StartAsync();
        Assert.Empty(_simulationService.CommentCalls); // Aucun appel IA automatique

        await vm.AskGuideCommand.ExecuteAsync(null);

        var call = Assert.Single(_simulationService.CommentCalls);
        Assert.Equal(7, call.PeriodId);
        Assert.NotNull(vm.Comment);
        Assert.True(vm.HasComment);
        Assert.Equal(2, vm.Candidates.Count);
        Assert.True(vm.Candidates[0].IsRecommended); // La recommandée en premier
    }

    [Fact]
    public async Task Simulation_AskGuide_Error_CommentLimit_ShowsSpecificMessage()
    {
        var vm = CreateViewModel();
        _simulationService.SimulationResult = SampleSimulation();
        _simulationService.CommentException = MakeApiException("COMMENT_LIMIT", (HttpStatusCode)429);

        await vm.StartAsync();
        await vm.AskGuideCommand.ExecuteAsync(null);

        Assert.Contains(_dialogService.ShownInfos, m => m.Contains("Le guide a déjà donné 10 avis aujourd'hui."));
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task Simulation_AskGuide_Error_AiUnavailable_KeepsSimulationRows()
    {
        var vm = CreateViewModel();
        _simulationService.SimulationResult = SampleSimulation();
        await vm.StartAsync();
        Assert.NotEmpty(vm.Rows);

        _simulationService.CommentException = new Exception("Erreur API IA");

        await vm.AskGuideCommand.ExecuteAsync(null);

        Assert.NotNull(vm.Simulation);
        Assert.NotEmpty(vm.Rows); // Les chiffres de la simulation restent affichés
        Assert.Single(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task Simulation_SaveAdvertising_Confirmed_CallsSavePlannedAsync()
    {
        var vm = CreateViewModel();
        _simulationService.SimulationResult = SampleSimulation();
        await vm.StartAsync();

        vm.AdvertisingText = "50,00"; // 5000 cents + 10000 existant = 15000
        _dialogService.ConfirmResult = true;

        await vm.SaveAdvertisingCommand.ExecuteAsync(null);

        Assert.Single(_dialogService.ConfirmCalls);
        Assert.Equal("Ajouter au budget", _dialogService.ConfirmCalls[0].Title);

        var call = Assert.Single(_budgetService.SavePlannedCalls);
        Assert.Equal(7, call.PeriodId);
        Assert.Equal("advertising", call.Category);
        Assert.Equal(15000L, call.PlannedCents);

        Assert.Null(vm.AdvertisingText);
        Assert.Contains(_dialogService.ShownInfos, m => m.Contains("Budget publicité mis à jour."));
    }

    [Fact]
    public async Task Simulation_SaveAdvertising_Declined_DoesNotSave()
    {
        var vm = CreateViewModel();
        _simulationService.SimulationResult = SampleSimulation();
        await vm.StartAsync();

        vm.AdvertisingText = "50,00";
        _dialogService.ConfirmResult = false; // Gérant refuse

        await vm.SaveAdvertisingCommand.ExecuteAsync(null);

        Assert.Single(_dialogService.ConfirmCalls);
        Assert.Empty(_budgetService.SavePlannedCalls);
    }

    [Fact]
    public async Task Simulation_ApplyCandidate_Confirmed_CallsSaveLinesAsyncAndRefreshes()
    {
        var vm = CreateViewModel();
        _simulationService.SimulationResult = SampleSimulation();
        await vm.StartAsync();

        var candidate = new SimulationCandidate
        {
            key = "opt1",
            label = "Optimisation",
            planned = new Dictionary<string, long>
            {
                ["purchases"] = 280000,
                ["advertising"] = 12000
            }
        };

        _dialogService.ConfirmResult = true;

        await vm.ApplyCandidateCommand.ExecuteAsync(candidate);

        Assert.Single(_dialogService.ConfirmCalls);
        Assert.Equal("Appliquer au budget", _dialogService.ConfirmCalls[0].Title);

        var call = Assert.Single(_budgetService.SaveLinesCalls);
        Assert.Equal(7, call.PeriodId);
        Assert.Equal(280000L, call.Lines["purchases"]);

        Assert.Contains(_dialogService.ShownInfos, m => m.Contains("Budget de la période mis à jour."));
    }

    [Fact]
    public async Task Simulation_Error_PeriodClosed_DisplaysApiErrorMessage()
    {
        var vm = CreateViewModel();
        _simulationService.SimulateException = MakeApiException("PERIOD_CLOSED", HttpStatusCode.Conflict, "Cette période est clôturée.");

        await vm.RunSimulationAsync();

        Assert.True(vm.HasError);
        Assert.Contains("Cette période est clôturée.", vm.ErrorText);
    }
}
