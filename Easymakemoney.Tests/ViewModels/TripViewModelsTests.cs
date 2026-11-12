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
public class TripViewModelsTests : IDisposable
{
    private readonly CultureInfo _originalCulture;
    private readonly FakeTripService _tripService = new();
    private readonly FakeDialogService _dialogService = new();

    public TripViewModelsTests()
    {
        _originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
        SiteCurrency.Reset();
        SiteCurrency.SetCurrency("CAD");
    }

    public void Dispose()
    {
        SiteCurrency.Reset();
        CultureInfo.CurrentCulture = _originalCulture;
    }

    private TripListViewModel CreateListViewModel() => new(_tripService, _dialogService);
    private TripFollowUpViewModel CreateFollowUpViewModel() => new(_tripService, _dialogService);
    private TripFormViewModel CreateFormViewModel() => new(_tripService, _dialogService);

    // =========================================================================
    // TripListViewModel
    // =========================================================================

    [Fact]
    public async Task TripList_LoadAsync_PopulatesTripsAndSetsIsEmptyFalse()
    {
        var vm = CreateListViewModel();
        _tripService.TripsResult = new List<Trip>
        {
            new() { id = 1, name = "Salon Paris", startDate = "2026-11-20", endDate = "2026-11-24", budgetTotal = 200000 },
            new() { id = 2, name = "Mission Tokyo", startDate = "2026-12-01", endDate = "2026-12-08", budgetTotal = 500000 }
        };

        await vm.LoadAsync();

        Assert.Equal(2, vm.Trips.Count);
        Assert.Equal("Salon Paris", vm.Trips[0].name);
        Assert.Equal("Mission Tokyo", vm.Trips[1].name);
        Assert.False(vm.IsEmpty);
        Assert.Single(_tripService.GetTripsCalls);
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task TripList_LoadAsync_Empty_SetsIsEmptyTrue()
    {
        var vm = CreateListViewModel();
        _tripService.TripsResult = new List<Trip>();

        await vm.LoadAsync();

        Assert.Empty(vm.Trips);
        Assert.True(vm.IsEmpty);
        Assert.Single(_tripService.GetTripsCalls);
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task TripList_LoadAsync_Error_ShowsErrorDialog()
    {
        var vm = CreateListViewModel();
        _tripService.GetTripsException = new HttpRequestException("Connexion impossible");

        await vm.LoadAsync();

        Assert.Empty(vm.Trips);
        var error = Assert.Single(_dialogService.ShownErrors);
        Assert.IsType<HttpRequestException>(error);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task TripList_Commands_WithoutShell_ExecuteSafely()
    {
        var vm = CreateListViewModel();
        var trip = new Trip { id = 3, name = "Visite Fournisseur" };

        var newTripEx = await Record.ExceptionAsync(() => vm.NewTripCommand.ExecuteAsync(null));
        var openEx = await Record.ExceptionAsync(() => vm.OpenCommand.ExecuteAsync(trip));

        Assert.Null(newTripEx);
        Assert.Null(openEx);
        Assert.Empty(_dialogService.ShownErrors);
    }

    // =========================================================================
    // TripFollowUpViewModel
    // =========================================================================

    [Fact]
    public async Task TripFollowUp_LoadAsync_LoadsTripAndSpendingDays()
    {
        var vm = CreateFollowUpViewModel();
        vm.TripId = 10;

        _tripService.TripResult = new Trip
        {
            id = 10,
            name = "Salon Lyon",
            startDate = "2026-11-08",
            endDate = "2026-11-10",
            budgetTotal = 100000
        };

        var followUp = JsonConvert.DeserializeObject<TripFollowUp>("""
        {
            "currency": "CAD", "budgetScope": "expenses", "inProgress": true, "elapsedDays": 2,
            "days": [
                { "date": "2026-11-08", "purchases": 0, "expenses": 15000, "spent": 15000, "budget": 33333 },
                { "date": "2026-11-09", "purchases": 50000, "expenses": 40000, "spent": 40000, "budget": 33333 }
            ],
            "totals": { "purchases": 50000, "expenses": 55000, "spent": 55000, "budget": 100000, "remaining": 45000, "allowanceToDate": 66666 },
            "status": "ok"
        }
        """)!;
        _tripService.FollowUpResult = followUp;

        await vm.LoadAsync();

        Assert.NotNull(vm.Trip);
        Assert.Equal("Salon Lyon", vm.Trip!.name);
        Assert.NotNull(vm.FollowUp);
        Assert.True(vm.HasFollowUp);
        Assert.Equal(2, vm.Days.Count);
        Assert.Single(_tripService.GetTripCalls);
        Assert.Equal(10, _tripService.GetTripCalls[0]);
        Assert.Single(_tripService.GetFollowUpCalls);
        Assert.Equal(10, _tripService.GetFollowUpCalls[0]);
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task TripFollowUp_LoadAsync_Error_ShowsErrorDialog()
    {
        var vm = CreateFollowUpViewModel();
        vm.TripId = 11;
        _tripService.GetTripException = new HttpRequestException("Erreur de suivi");

        await vm.LoadAsync();

        Assert.Null(vm.Trip);
        Assert.Null(vm.FollowUp);
        Assert.False(vm.HasFollowUp);
        var error = Assert.Single(_dialogService.ShownErrors);
        Assert.IsType<HttpRequestException>(error);
    }

    [Fact]
    public async Task TripFollowUp_Delete_ConfirmationRefused_DoesNotCallService()
    {
        var vm = CreateFollowUpViewModel();
        vm.TripId = 12;
        _dialogService.ConfirmResult = false; // Gérant refuse la suppression

        await vm.DeleteCommand.ExecuteAsync(null);

        Assert.Single(_dialogService.ConfirmCalls);
        Assert.Contains("Supprimer le voyage", _dialogService.ConfirmCalls[0].Title);
        Assert.Empty(_tripService.DeleteCalls);
    }

    [Fact]
    public async Task TripFollowUp_Delete_Confirmed_CallsDeleteAsync()
    {
        var vm = CreateFollowUpViewModel();
        vm.TripId = 13;
        _dialogService.ConfirmResult = true; // Gérant confirme

        await vm.DeleteCommand.ExecuteAsync(null);

        var deleteCall = Assert.Single(_tripService.DeleteCalls);
        Assert.Equal(13, deleteCall);
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task TripFollowUp_Delete_Error_ShowsErrorDialog()
    {
        var vm = CreateFollowUpViewModel();
        vm.TripId = 14;
        _dialogService.ConfirmResult = true;
        _tripService.DeleteException = new HttpRequestException("Suppression échouée");

        await vm.DeleteCommand.ExecuteAsync(null);

        Assert.Single(_tripService.DeleteCalls);
        var error = Assert.Single(_dialogService.ShownErrors);
        Assert.IsType<HttpRequestException>(error);
    }

    // =========================================================================
    // TripFormViewModel
    // =========================================================================

    [Fact]
    public void TripForm_Init_CreateMode_InitializesDefaults()
    {
        var vm = CreateFormViewModel();

        Assert.Equal(0, vm.TripId);
        Assert.False(vm.IsEdit);
        Assert.Equal("Nouveau voyage", vm.PageTitle);
        Assert.Contains(TripFormViewModel.SiteCurrencyChoice, vm.CurrencyChoices);
        Assert.False(vm.NeedsExchangeRate);
        Assert.Equal("Budget total", vm.BudgetLabel);
    }

    [Fact]
    public async Task TripForm_LoadAsync_EditMode_PopulatesFieldsAndCurrency()
    {
        var vm = CreateFormViewModel();
        _tripService.TripResult = new Trip
        {
            id = 42,
            name = "Mission Milan",
            place = "Milan",
            startDate = "2026-11-20",
            endDate = "2026-11-25",
            budgetTotal = 250000,
            budgetDaily = 50000,
            budgetDailyEntered = true,
            currency = "EUR",
            exchangeRate = 1.48,
            siteCurrency = "CAD",
            notes = "Hôtel réservé"
        };

        await vm.LoadAsync(42);

        Assert.Equal(42, vm.TripId);
        Assert.True(vm.IsEdit);
        Assert.Equal("Modifier le voyage", vm.PageTitle);
        Assert.Equal("Mission Milan", vm.Name);
        Assert.Equal("Milan", vm.Place);
        Assert.Equal(new DateTime(2026, 11, 20), vm.StartDate);
        Assert.Equal(new DateTime(2026, 11, 25), vm.EndDate);
        Assert.Equal("6 jours", vm.DaysText);
        Assert.Equal("2500", vm.BudgetTotalText);
        Assert.Equal("500", vm.BudgetDailyText);
        Assert.Equal("EUR", vm.SelectedCurrency);
        Assert.True(vm.NeedsExchangeRate);
        Assert.Equal("1,48", vm.ExchangeRateText);
        Assert.Equal("Hôtel réservé", vm.Notes);
    }

    [Theory]
    [InlineData("", "100", "Donnez un nom au voyage")]
    [InlineData("   ", "100", "Donnez un nom au voyage")]
    [InlineData("Voyage", "", "Saisissez le budget total du voyage")]
    [InlineData("Voyage", "0", "Saisissez le budget total du voyage")]
    [InlineData("Voyage", "-50", "Saisissez le budget total du voyage")]
    public async Task TripForm_Save_ValidationFailures_DoNotSendRequest(string name, string budget, string expectedMessage)
    {
        var vm = CreateFormViewModel();
        vm.Name = name;
        vm.BudgetTotalText = budget;

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Contains(expectedMessage, Assert.Single(_dialogService.ShownInfos));
        Assert.Empty(_tripService.CreateCalls);
        Assert.Empty(_tripService.UpdateCalls);
    }

    [Fact]
    public async Task TripForm_Save_EndDateBeforeStartDate_ShowsInfoAndDoesNotSend()
    {
        var vm = CreateFormViewModel();
        vm.Name = "Voyage Test";
        vm.BudgetTotalText = "100";
        vm.StartDate = DateTime.Today.AddDays(5);
        vm.EndDate = DateTime.Today;

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Contains("La date de fin doit suivre la date de début.", Assert.Single(_dialogService.ShownInfos));
        Assert.Empty(_tripService.CreateCalls);
    }

    [Fact]
    public async Task TripForm_Save_ForeignCurrencyWithoutRate_ShowsInfoAndDoesNotSend()
    {
        var vm = CreateFormViewModel();
        vm.Name = "Voyage Paris";
        vm.BudgetTotalText = "1500";
        vm.SelectedCurrency = "EUR";
        Assert.True(vm.NeedsExchangeRate);
        vm.ExchangeRateText = ""; // Taux manquant

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Contains("Saisissez le taux de change", Assert.Single(_dialogService.ShownInfos));
        Assert.Empty(_tripService.CreateCalls);
    }

    [Fact]
    public async Task TripForm_Save_Create_SiteCurrency_OmitsCurrencyAndRate()
    {
        var vm = CreateFormViewModel();
        vm.Name = "Voyage Montréal";
        vm.Place = "Montréal";
        vm.StartDate = new DateTime(2026, 11, 15);
        vm.EndDate = new DateTime(2026, 11, 18);
        vm.BudgetTotalText = "1200,50";
        vm.SelectedCurrency = TripFormViewModel.SiteCurrencyChoice; // Devise du site

        await vm.SaveCommand.ExecuteAsync(null);

        var call = Assert.Single(_tripService.CreateCalls);
        Assert.Equal("Voyage Montréal", call.name);
        Assert.Equal("Montréal", call.place);
        Assert.Equal("2026-11-15", call.startDate);
        Assert.Equal("2026-11-18", call.endDate);
        Assert.Equal(120050, call.budgetTotal); // En centimes
        Assert.Null(call.budgetDaily);
        Assert.Null(call.currency); // Devise du site non transmise
        Assert.Null(call.exchangeRate);
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task TripForm_Save_Edit_ForeignCurrency_SendsCurrencyAndRate()
    {
        var vm = CreateFormViewModel();
        _tripService.TripResult = new Trip
        {
            id = 5,
            name = "Ancien Nom",
            startDate = "2026-11-20",
            endDate = "2026-11-22",
            budgetTotal = 100000
        };
        await vm.LoadAsync(5);

        vm.Name = "Salon Bruxelles";
        vm.BudgetTotalText = "2000,00";
        vm.BudgetDailyText = "666,66";
        vm.SelectedCurrency = "EUR";
        vm.ExchangeRateText = "1,48";

        await vm.SaveCommand.ExecuteAsync(null);

        var (tripId, request) = Assert.Single(_tripService.UpdateCalls);
        Assert.Equal(5, tripId);
        Assert.Equal("Salon Bruxelles", request.name);
        Assert.Equal(200000, request.budgetTotal);
        Assert.Equal(66666, request.budgetDaily);
        Assert.Equal("EUR", request.currency);
        Assert.Equal(1.48, request.exchangeRate);
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task TripForm_Save_ApiConflict_TripOverlap_ShowsErrorDialogWithSpecificMessage()
    {
        var vm = CreateFormViewModel();
        vm.Name = "Salon Berlin";
        vm.BudgetTotalText = "1500";

        // Simule le conflit 409 TRIP_OVERLAP
        var apiError = ApiError.FromHttpResponse("""{ "error": "TRIP_OVERLAP" }""", HttpStatusCode.Conflict, null);
        _tripService.CreateException = new ApiException(apiError, HttpStatusCode.Conflict);

        await vm.SaveCommand.ExecuteAsync(null);

        var error = Assert.Single(_dialogService.ShownErrors);
        Assert.IsType<ApiException>(error);
        // Vérifie le texte réellement présenté au gérant (message de secours de ApiError)
        Assert.Equal("Ces dates chevauchent un autre voyage.", error.Message);
    }
}
