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
public class PreparationViewModelTests : IDisposable
{
    private readonly CultureInfo _originalCulture;
    private readonly FakePreparationService _preparationService = new();
    private readonly FakePeriodService _periodService = new();
    private readonly FakeDialogService _dialogService = new();

    public PreparationViewModelTests()
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

    private PreparationViewModel CreateViewModel()
    {
        return new PreparationViewModel(_preparationService, _periodService, _dialogService);
    }

    private static PreparationEstimates SampleEstimates()
    {
        const string json = """
        {
          "period": { "id": 8, "name": "Hiver 2026", "start": "2026-12-01", "end": "2027-02-28" },
          "days": 89,
          "lastYear": { "revenue": 350000 },
          "pace": { "revenue": 380000 },
          "recommended": { "method": "lastYear", "reason": "Ventes stables l'an dernier." }
        }
        """;
        return JsonConvert.DeserializeObject<PreparationEstimates>(json)!;
    }

    private static PreparationSimulation SampleSimulation(string hash = "hash123")
    {
        var json = $$"""
        {
          "currency": "CAD",
          "previewHash": "{{hash}}",
          "plan": {
            "sales": { "target": 350000, "planned": 350000 },
            "grossMargin": 140000,
            "result": 25000
          },
          "planned": [
            { "period": { "id": 8, "name": "Hiver 2026" }, "purchases": 150000, "mode": "replace" }
          ]
        }
        """;
        return JsonConvert.DeserializeObject<PreparationSimulation>(json)!;
    }

    private static PreparationAdvice SampleAdvice()
    {
        return new PreparationAdvice
        {
            candidates = new List<PreparationCandidate>
            {
                new() { key = "c1", label = "Plan prudent", IsRecommended = true },
                new() { key = "c2", label = "Plan ambitieux", IsRecommended = false }
            }
        };
    }

    private static ApiException MakeApiException(string code, HttpStatusCode statusCode)
    {
        var apiError = ApiError.FromHttpResponse($$"""{ "error": "{{code}}", "code": "{{code}}" }""", statusCode, null);
        return new ApiException(apiError, statusCode);
    }

    private static ListCollection SamplePeriod(int id = 7, string end = "2026-11-30") => new()
    {
        id = id,
        nomCollection = "Automne 2026",
        startDateCollection = "2026-09-01",
        endDateCollection = end,
        photoCollection = ""
    };

    [Fact]
    public async Task Preparation_Start_LoadsEstimatesForTargetPeriod()
    {
        var vm = CreateViewModel();
        _periodService.CurrentPeriodResult = SamplePeriod();
        _preparationService.EstimatesResult = SampleEstimates();

        await vm.StartAsync();

        var call = Assert.Single(_preparationService.GetEstimatesCalls);
        Assert.Equal("2026-12-01", call); // Jour suivant la fin de la période courante
        Assert.NotNull(vm.PeriodTitle);
        Assert.Contains("Hiver 2026", vm.PeriodTitle);
        Assert.NotEmpty(vm.SalesMethods);
        Assert.Equal("lastYear", vm.SelectedSalesMethod?.Code);
        Assert.Equal("Ventes stables l'an dernier.", vm.RecommendationText);
    }

    [Fact]
    public async Task Preparation_TargetChange_ReloadsEstimatesAndClearsPreviousResult()
    {
        var vm = CreateViewModel();
        _periodService.CurrentPeriodResult = SamplePeriod();
        _preparationService.EstimatesResult = SampleEstimates();
        _preparationService.SimulationResult = SampleSimulation();

        await vm.StartAsync();
        await vm.SimulateCommand.ExecuteAsync(null);
        Assert.NotNull(vm.Simulation);

        // Changement de cible : "current"
        vm.SelectedTarget = vm.Targets[0];

        // L'ancien résultat est effacé
        Assert.Null(vm.Simulation);
        Assert.Empty(vm.Rows);
        Assert.Equal(2, _preparationService.GetEstimatesCalls.Count);
    }

    [Fact]
    public async Task Preparation_AddPurchase_ValidAmount_AddsToCollectionAndClearsInputs()
    {
        var vm = CreateViewModel();
        _preparationService.EstimatesResult = SampleEstimates();
        await vm.StartAsync();

        vm.PurchaseAmountText = "2500,00";
        vm.PurchaseCoefficientText = "2,5";

        await vm.AddPurchaseCommand.ExecuteAsync(null);

        var purchase = Assert.Single(vm.Purchases);
        Assert.Equal(250000L, purchase.AmountCents);
        Assert.Equal(2.5, purchase.Coefficient);
        Assert.Null(vm.PurchaseAmountText);
        Assert.Null(vm.PurchaseCoefficientText);
    }

    [Fact]
    public async Task Preparation_AddPurchase_InvalidAmountOrCoefficient_ShowsInfoDialog()
    {
        var vm = CreateViewModel();
        _preparationService.EstimatesResult = SampleEstimates();
        await vm.StartAsync();

        // 1. Montant invalide (0)
        vm.PurchaseAmountText = "0";
        await vm.AddPurchaseCommand.ExecuteAsync(null);

        Assert.Empty(vm.Purchases);
        Assert.Contains(_dialogService.ShownInfos, m => m.Contains("Saisissez le montant de l'achat prévu."));

        // 2. Coefficient hors limites (> 20)
        vm.PurchaseAmountText = "500";
        vm.PurchaseCoefficientText = "25";
        await vm.AddPurchaseCommand.ExecuteAsync(null);

        Assert.Empty(vm.Purchases);
        Assert.Contains(_dialogService.ShownInfos, m => m.Contains("Le coefficient doit être entre 1 et 20."));
    }

    [Fact]
    public async Task Preparation_Simulate_ValidRequest_CallsSimulateAsyncAndPopulatesRows()
    {
        var vm = CreateViewModel();
        _periodService.CurrentPeriodResult = SamplePeriod();
        _preparationService.EstimatesResult = SampleEstimates();
        _preparationService.SimulationResult = SampleSimulation();

        await vm.StartAsync();
        vm.Purchases.Add(new PreparationPurchase { AmountCents = 150000L });

        await vm.SimulateCommand.ExecuteAsync(null);

        var request = Assert.Single(_preparationService.SimulateCalls);
        Assert.Equal("2026-12-01", request.Date);
        Assert.Single(request.Purchases);
        Assert.NotNull(vm.Simulation);
        Assert.True(vm.HasSimulation);
        Assert.NotEmpty(vm.Rows);
    }

    [Fact]
    public async Task Preparation_Simulate_InvalidTripDates_ShowsInfoDialog()
    {
        var vm = CreateViewModel();
        _preparationService.EstimatesResult = SampleEstimates();
        await vm.StartAsync();

        vm.HasTrip = true;
        vm.TripStart = DateTime.Today.AddDays(10);
        vm.TripEnd = DateTime.Today.AddDays(5); // Fin avant début

        await vm.SimulateCommand.ExecuteAsync(null);

        Assert.Empty(_preparationService.SimulateCalls);
        Assert.Contains(_dialogService.ShownInfos, m => m.Contains("La fin du voyage doit suivre son début."));
    }

    [Fact]
    public async Task Preparation_AskGuide_RequiresPreviousSimulation_CallsAdviseAsync()
    {
        var vm = CreateViewModel();
        _preparationService.EstimatesResult = SampleEstimates();
        _preparationService.SimulationResult = SampleSimulation();
        _preparationService.AdviceResult = SampleAdvice();

        await vm.StartAsync();

        // 1. Sans simulation préalable : rien ne part
        await vm.AskGuideCommand.ExecuteAsync(null);
        Assert.Empty(_preparationService.AdviseCalls);

        // 2. Avec simulation préalable
        await vm.SimulateCommand.ExecuteAsync(null);
        await vm.AskGuideCommand.ExecuteAsync(null);

        Assert.Single(_preparationService.AdviseCalls);
        Assert.NotNull(vm.Advice);
        Assert.True(vm.HasAdvice);
        Assert.Equal(2, vm.Candidates.Count);
        Assert.True(vm.Candidates[0].IsRecommended); // Recommandé en premier
    }

    [Fact]
    public async Task Preparation_Validate_Confirmed_CallsValidateAsyncAndNavigatesBack()
    {
        var vm = CreateViewModel();
        _preparationService.EstimatesResult = SampleEstimates();
        _preparationService.SimulationResult = SampleSimulation("hash-valid-123");

        await vm.StartAsync();
        await vm.SimulateCommand.ExecuteAsync(null);

        _dialogService.ConfirmResult = true;

        await vm.ValidateCommand.ExecuteAsync(null);

        Assert.Single(_dialogService.ConfirmCalls);
        Assert.Equal("Valider ce plan", _dialogService.ConfirmCalls[0].Title);

        var (request, hash) = Assert.Single(_preparationService.ValidateCalls);
        Assert.Equal("hash-valid-123", hash);
        Assert.Contains(_dialogService.ShownInfos, m => m.Contains("Plan validé : le budget est enregistré"));
    }

    [Fact]
    public async Task Preparation_Validate_PricesChanged_ShowsWarningAndResimulates()
    {
        var vm = CreateViewModel();
        _preparationService.EstimatesResult = SampleEstimates();
        _preparationService.SimulationResult = SampleSimulation("hash-obsolete");

        await vm.StartAsync();
        await vm.SimulateCommand.ExecuteAsync(null);

        _preparationService.ValidateException = MakeApiException("PRICES_CHANGED", HttpStatusCode.Conflict);
        _dialogService.ConfirmResult = true;

        await vm.ValidateCommand.ExecuteAsync(null);

        Assert.Contains(_dialogService.ShownInfos, m => m.Contains("Le stock, les coûts ou la trésorerie ont changé"));
        // Resimulation automatique déclenchée après PRICES_CHANGED
        Assert.Equal(2, _preparationService.SimulateCalls.Count);
    }
}
