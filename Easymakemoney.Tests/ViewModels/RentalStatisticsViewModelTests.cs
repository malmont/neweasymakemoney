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

    public RentalStatisticsViewModelTests()
    {
        CultureInfo.CurrentCulture = new CultureInfo("fr-CA");
        SiteCurrency.Reset();
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _originalCulture;
        SiteCurrency.Reset();
    }

    private RentalStatisticsViewModel CreateViewModel() =>
        new(_rentalService, _periodService, _dialogService);

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
}
