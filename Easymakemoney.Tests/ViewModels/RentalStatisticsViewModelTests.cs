using System.Collections.ObjectModel;
using System.Globalization;
using Easymakemoney.Models;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.Utilities;
using Easymakemoney.ViewModels.Statistiques;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

[Collection("SiteCurrency")]
public class RentalStatisticsViewModelTests : IDisposable
{
    private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;
    private readonly FakeRentalStatisticsService _rentalService = new();
    private readonly FakeListCollectionService _periodService = new();
    private readonly FakeDialogService _dialogService = new();
    private readonly FakeRentalPromotionService _promotionService = new();

    public RentalStatisticsViewModelTests()
    {
        CultureInfo.CurrentCulture = new CultureInfo("fr-CA");
        SiteCurrency.Reset();
        RentalStatisticsViewModel.PromotionsChanged = false;
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _originalCulture;
        SiteCurrency.Reset();
        RentalStatisticsViewModel.PromotionsChanged = false;
    }

    private RentalStatisticsViewModel CreateViewModel() =>
        new(_rentalService, _periodService, _dialogService);

    private RentalStatisticsViewModel CreateViewModelWithPromotions() =>
        new(_rentalService, _periodService, _dialogService, _promotionService);

    private static RentalStatistics CreateSampleRental()
    {
        return new RentalStatistics
        {
            currency = "CAD",
            revenue = 31900,
            products = new List<RentalProduct>
            {
                new() { productId = 1, name = "Kayak", revenue = 20000, rentals = 10, occupancy = 45.5 }
            },
            weekdays = new List<RentalWeekday>
            {
                new() { weekday = 1, label = "Lundi", rentals = 2, occupancy = 50.0 },
                new() { weekday = 2, label = "Mardi", rentals = 1, occupancy = 25.0 }
            }
        };
    }

    [Fact]
    public async Task InitializeAsync_LoadsOnceWithoutRange()
    {
        _rentalService.ResultToReturn = CreateSampleRental();
        var vm = CreateViewModel();

        await vm.InitializeAsync();
        await vm.InitializeAsync(); // Second appel : ne recharge pas

        var url = Assert.Single(_rentalService.RequestedUrls);
        Assert.Equal("gestion/locations/statistiques?locale=fr", url);
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task SelectRange_30d_UsesDatesRelativeToToday()
    {
        _rentalService.ResultToReturn = CreateSampleRental();
        var vm = CreateViewModel();

        await vm.SelectRangeCommand.ExecuteAsync("30d");

        var from = DateTime.Today.AddDays(-29);
        var to = DateTime.Today;
        var expectedUrl = $"gestion/locations/statistiques?from={TreasuryDates.Iso(from)}&to={TreasuryDates.Iso(to)}&locale=fr";
        Assert.Equal(expectedUrl, _rentalService.RequestedUrls.Last());
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task SelectRange_Period_SendsPeriodIdAfterSelection()
    {
        var period = new ListCollection { id = 15, nomCollection = "Automne", photoCollection = "", startDateCollection = "2026-09-01", endDateCollection = "2026-11-30" };
        _periodService.CollectionsToReturn = new ObservableCollection<ListCollection> { period };
        _rentalService.ResultToReturn = CreateSampleRental();
        var vm = CreateViewModel();

        await vm.SelectRangeCommand.ExecuteAsync("period");
        Assert.Empty(_rentalService.RequestedUrls);

        vm.SelectedPeriod = period;

        Assert.Equal("gestion/locations/statistiques?periodId=15&locale=fr", _rentalService.RequestedUrls.Last());
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task SelectRange_Custom_ValidatesDatesBeforeSending()
    {
        _rentalService.ResultToReturn = CreateSampleRental();
        var vm = CreateViewModel();

        await vm.SelectRangeCommand.ExecuteAsync("custom");
        Assert.Empty(_rentalService.RequestedUrls);

        // Date de fin avant date de début
        vm.CustomFrom = DateTime.Today;
        vm.CustomTo = DateTime.Today.AddDays(-1);
        await vm.ApplyCustomDatesCommand.ExecuteAsync(null);
        Assert.Contains("La date de fin doit suivre la date de début.", _dialogService.ShownInfos);
        Assert.Empty(_rentalService.RequestedUrls);

        // Plus de 366 jours
        vm.CustomFrom = DateTime.Today.AddDays(-400);
        vm.CustomTo = DateTime.Today;
        await vm.ApplyCustomDatesCommand.ExecuteAsync(null);
        Assert.Contains("Choisissez 366 jours au plus.", _dialogService.ShownInfos);
        Assert.Empty(_rentalService.RequestedUrls);

        // Dates valides
        var from = DateTime.Today.AddDays(-10);
        var to = DateTime.Today;
        vm.CustomFrom = from;
        vm.CustomTo = to;
        await vm.ApplyCustomDatesCommand.ExecuteAsync(null);

        var expectedUrl = $"gestion/locations/statistiques?from={TreasuryDates.Iso(from)}&to={TreasuryDates.Iso(to)}&locale=fr";
        Assert.Equal(expectedUrl, _rentalService.RequestedUrls.Last());
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task WeekdayPoints_And_SelectedDayText()
    {
        _rentalService.ResultToReturn = CreateSampleRental();
        var vm = CreateViewModel();

        await vm.InitializeAsync();

        Assert.Equal("Touchez un jour pour lire son occupation moyenne.", vm.SelectedDayText);
        Assert.NotEmpty(vm.WeekdayPoints);

        vm.SelectedDayIndex = 0;
        Assert.Equal(vm.WeekdayPoints[0].Label, vm.SelectedDayText);
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task LoadPromotions_FiltersOnlyActiveAndUpcoming_IncludingEndedToday()
    {
        var today = DateTime.Today; // Toutes les dates partent de DateTime.Today
        _rentalService.ResultToReturn = CreateSampleRental();
        _promotionService.PromotionsResult = new List<RentalPromotionItem>
        {
            new() { id = 1, batchId = 10, name = "Kayak", status = "active", startDate = TreasuryDates.Iso(today.AddDays(-5)), endDate = TreasuryDates.Iso(today.AddDays(10)) },
            new() { id = 2, batchId = 20, name = "Vélo", status = "upcoming", startDate = TreasuryDates.Iso(today.AddDays(2)), endDate = TreasuryDates.Iso(today.AddDays(20)) },
            new() { id = 3, batchId = 30, name = "Tente", status = "cancelled", startDate = TreasuryDates.Iso(today.AddDays(-10)), endDate = TreasuryDates.Iso(today) },
            new() { id = 4, batchId = 40, name = "Planche", status = "ended", startDate = TreasuryDates.Iso(today.AddDays(-20)), endDate = TreasuryDates.Iso(today.AddDays(-1)) },
            new() { id = 5, batchId = 50, name = "Pédalo", status = "active", endedAt = today.ToString("yyyy-MM-ddTHH:mm:sszzz"), startDate = TreasuryDates.Iso(today.AddDays(-3)), endDate = TreasuryDates.Iso(today.AddDays(5)) }
        };

        var vm = CreateViewModelWithPromotions();
        await vm.InitializeAsync();

        // Seules les promotions active, upcoming et active se terminant ce soir sont visibles
        Assert.Equal(3, vm.Promotions.Count);
        Assert.Equal(new[] { 1, 2, 5 }, vm.Promotions.Select(p => p.id));
        Assert.True(vm.HasPromotions);

        var endingToday = vm.Promotions.First(p => p.id == 5);
        Assert.Equal("Se termine ce soir", endingToday.StatusText);
        Assert.False(endingToday.CanEnd);
    }

    [Fact]
    public async Task EndPromotion_CannotEnd_DoesNothing()
    {
        var today = DateTime.Today;
        _rentalService.ResultToReturn = CreateSampleRental();
        var promoEndingToday = new RentalPromotionItem
        {
            id = 5,
            batchId = 50,
            status = "active",
            endedAt = today.ToString("yyyy-MM-ddTHH:mm:sszzz")
        };
        Assert.False(promoEndingToday.CanEnd);

        var vm = CreateViewModelWithPromotions();
        await vm.EndPromotionCommand.ExecuteAsync(promoEndingToday);

        Assert.Empty(_dialogService.ConfirmCalls);
        Assert.Empty(_promotionService.EndCalls);
    }

    [Fact]
    public async Task EndPromotion_ConfirmationRefused_DoesNotSendRequest()
    {
        var today = DateTime.Today;
        _rentalService.ResultToReturn = CreateSampleRental();
        var promo = new RentalPromotionItem
        {
            id = 1,
            batchId = 10,
            name = "Kayak",
            status = "active",
            startDate = TreasuryDates.Iso(today)
        };
        Assert.True(promo.CanEnd);

        var vm = CreateViewModelWithPromotions();
        _dialogService.ConfirmResult = false;

        await vm.EndPromotionCommand.ExecuteAsync(promo);

        Assert.Single(_dialogService.ConfirmCalls);
        Assert.Empty(_promotionService.EndCalls);
    }

    [Fact]
    public async Task EndPromotion_Confirmed_DifferentiatesUpcomingAndActiveMessages()
    {
        var today = DateTime.Today;
        _rentalService.ResultToReturn = CreateSampleRental();
        var upcomingPromo = new RentalPromotionItem
        {
            id = 2,
            batchId = 20,
            name = "Vélo",
            percent = 15,
            status = "upcoming",
            startDate = TreasuryDates.Iso(today.AddDays(5))
        };
        var activePromo = new RentalPromotionItem
        {
            id = 1,
            batchId = 10,
            name = "Kayak",
            percent = 20,
            status = "active",
            startDate = TreasuryDates.Iso(today.AddDays(-2))
        };

        var vm = CreateViewModelWithPromotions();
        _dialogService.ConfirmResult = true;

        // Fin d'une promotion à venir
        await vm.EndPromotionCommand.ExecuteAsync(upcomingPromo);
        var upcomingConfirm = Assert.Single(_dialogService.ConfirmCalls);
        Assert.Contains("n'a pas commencé : elle ne s'appliquera jamais", upcomingConfirm.Message);
        Assert.Equal(20, Assert.Single(_promotionService.EndCalls));

        // Fin d'une promotion en cours
        await vm.EndPromotionCommand.ExecuteAsync(activePromo);
        var activeConfirm = _dialogService.ConfirmCalls.Last();
        Assert.Contains("s'arrête ce soir. Une location qui commence aujourd'hui la garde", activeConfirm.Message);
        Assert.Equal(10, _promotionService.EndCalls.Last());
    }

    [Fact]
    public async Task InitializeAsync_WhenPromotionsChanged_ReloadsOnlyPromotions()
    {
        _rentalService.ResultToReturn = CreateSampleRental();
        var vm = CreateViewModelWithPromotions();

        await vm.InitializeAsync();
        Assert.Single(_rentalService.RequestedUrls);
        Assert.Equal(1, _promotionService.GetPromotionsCalls);

        // Drapeau activé après création d'une promotion
        RentalStatisticsViewModel.PromotionsChanged = true;

        await vm.InitializeAsync();

        // Les statistiques ne sont PAS rechargées (toujours 1 seul appel d'URL)
        Assert.Single(_rentalService.RequestedUrls);
        // Les promotions sont rechargées (2 appels)
        Assert.Equal(2, _promotionService.GetPromotionsCalls);
        // Le drapeau est réinitialisé à faux
        Assert.False(RentalStatisticsViewModel.PromotionsChanged);
    }
}
