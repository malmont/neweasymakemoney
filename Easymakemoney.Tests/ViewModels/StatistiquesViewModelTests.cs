using System.Collections.ObjectModel;
using System.Globalization;
using Easymakemoney.Models;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.Utilities;
using Easymakemoney.ViewModels.Statistiques;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

[Collection("SiteCurrency")]
public class StatistiquesViewModelTests : IDisposable
{
    private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;
    private readonly FakeUnifiedStatisticsService _statsService = new();
    private readonly FakeListCollectionService _periodService = new();
    private readonly FakeDialogService _dialogService = new();

    public StatistiquesViewModelTests()
    {
        CultureInfo.CurrentCulture = new CultureInfo("fr-CA");
        SiteCurrency.Reset();
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _originalCulture;
        SiteCurrency.Reset();
    }

    private StatistiquesViewModel CreateViewModel() =>
        new(_statsService, _periodService, _dialogService);

    private static UnifiedStatistics CreateSampleData()
    {
        return new UnifiedStatistics
        {
            currency = "CAD",
            comparison = new StatsComparison
            {
                currentLabel = "1-11 nov.",
                previousLabel = "1-11 oct.",
                lastYearLabel = "1-11 nov. 2025"
            },
            indicators = new StatsIndicators
            {
                revenue = new StatsIndicator { value = 117070, unit = "money" },
                grossMargin = new StatsIndicator { value = 51225, unit = "money" },
                orders = new StatsIndicator { value = 12, unit = "count" }
            },
            chart = new StatsChart
            {
                granularity = "day",
                points = new List<StatsPoint>
                {
                    new()
                    {
                        key = "2026-11-01",
                        from = "2026-11-01",
                        to = "2026-11-01",
                        revenue = 5000,
                        orders = 2,
                        lastYear = new StatsPointValues { revenue = 3000, orders = 1 }
                    }
                }
            }
        };
    }

    [Fact]
    public async Task InitializeAsync_LoadsOnceWithDefaultQuery()
    {
        _statsService.ResultToReturn = CreateSampleData();
        var vm = CreateViewModel();

        await vm.InitializeAsync();
        await vm.InitializeAsync(); // Second appel : _loader ne doit pas recharger

        var url = Assert.Single(_statsService.RequestedUrls);
        Assert.Equal("gestion/statistiques?range=month&source=all&locale=fr", url);
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Theory]
    [InlineData("today", "gestion/statistiques?range=today&source=all&locale=fr")]
    [InlineData("7d", "gestion/statistiques?range=7d&source=all&locale=fr")]
    [InlineData("month", "gestion/statistiques?range=month&source=all&locale=fr")]
    [InlineData("year", "gestion/statistiques?range=year&source=all&locale=fr")]
    public async Task SelectRange_UpdatesUrl_ForStandardRanges(string code, string expectedUrl)
    {
        _statsService.ResultToReturn = CreateSampleData();
        var vm = CreateViewModel();

        await vm.SelectRangeCommand.ExecuteAsync(code);

        Assert.Contains(expectedUrl, _statsService.RequestedUrls);
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task SelectSource_KeepsCurrentRange()
    {
        _statsService.ResultToReturn = CreateSampleData();
        var vm = CreateViewModel();

        await vm.SelectRangeCommand.ExecuteAsync("7d");
        await vm.SelectSourceCommand.ExecuteAsync("pos");

        Assert.Equal("gestion/statistiques?range=7d&source=pos&locale=fr", _statsService.RequestedUrls.Last());
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task SelectRange_Period_SelectsPeriodContainingToday_AndQueriesPeriodId()
    {
        var today = DateTime.Today;
        var pastPeriod = new ListCollection
        {
            id = 10,
            nomCollection = "Passée",
            photoCollection = "",
            startDateCollection = TreasuryDates.Iso(today.AddDays(-60)),
            endDateCollection = TreasuryDates.Iso(today.AddDays(-30))
        };
        var currentPeriod = new ListCollection
        {
            id = 42,
            nomCollection = "En cours",
            photoCollection = "",
            startDateCollection = TreasuryDates.Iso(today.AddDays(-10)),
            endDateCollection = TreasuryDates.Iso(today.AddDays(20))
        };
        _periodService.CollectionsToReturn = new ObservableCollection<ListCollection> { pastPeriod, currentPeriod };
        _statsService.ResultToReturn = CreateSampleData();
        var vm = CreateViewModel();

        await vm.SelectRangeCommand.ExecuteAsync("period");

        Assert.NotNull(vm.SelectedPeriod);
        Assert.Equal(42, vm.SelectedPeriod.id);
        Assert.Contains("gestion/statistiques?periodId=42&source=all&locale=fr", _statsService.RequestedUrls);
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task SelectRange_Custom_DoesNotSendRequestUntilApplyCustomDates_AndValidatesDates()
    {
        _statsService.ResultToReturn = CreateSampleData();
        var vm = CreateViewModel();

        await vm.SelectRangeCommand.ExecuteAsync("custom");
        Assert.Empty(_statsService.RequestedUrls);

        // Date fin avant date début
        vm.CustomFrom = DateTime.Today;
        vm.CustomTo = DateTime.Today.AddDays(-1);
        await vm.ApplyCustomDatesCommand.ExecuteAsync(null);
        Assert.Contains("La date de fin doit suivre la date de début.", _dialogService.ShownInfos);
        Assert.Empty(_statsService.RequestedUrls);

        // Plus de 366 jours
        vm.CustomFrom = DateTime.Today.AddDays(-400);
        vm.CustomTo = DateTime.Today;
        await vm.ApplyCustomDatesCommand.ExecuteAsync(null);
        Assert.Contains("Choisissez 366 jours au plus.", _dialogService.ShownInfos);
        Assert.Empty(_statsService.RequestedUrls);

        // Dates valides
        var from = DateTime.Today.AddDays(-14);
        var to = DateTime.Today;
        vm.CustomFrom = from;
        vm.CustomTo = to;
        await vm.ApplyCustomDatesCommand.ExecuteAsync(null);

        var expectedUrl = $"gestion/statistiques?from={TreasuryDates.Iso(from)}&to={TreasuryDates.Iso(to)}&source=all&locale=fr";
        Assert.Equal(expectedUrl, _statsService.RequestedUrls.Last());
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task SelectMetric_Orders_SwitchesChartIsMoneyAndPoints_WithoutNewRequest()
    {
        _statsService.ResultToReturn = CreateSampleData();
        var vm = CreateViewModel();
        await vm.InitializeAsync();
        var initialRequestCount = _statsService.RequestedUrls.Count;

        Assert.True(vm.ChartIsMoney);

        vm.SelectMetricCommand.Execute("orders");

        Assert.False(vm.ChartIsMoney);
        Assert.Equal(initialRequestCount, _statsService.RequestedUrls.Count);
        Assert.NotEmpty(vm.ChartPoints);
        Assert.Equal(2, vm.ChartPoints[0].Value);
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task SelectedPointText_ReflectsSelection()
    {
        _statsService.ResultToReturn = CreateSampleData();
        var vm = CreateViewModel();
        await vm.InitializeAsync();

        Assert.Equal("Touchez une barre pour lire sa valeur. En gris : l'an dernier aux mêmes dates.", vm.SelectedPointText);

        vm.SelectedPointIndex = 0;

        Assert.Contains("1 nov.", vm.SelectedPointText);
        Assert.Contains("an dernier", vm.SelectedPointText);
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task LoadAsync_OnError_CallsShowErrorAsync()
    {
        _statsService.ExceptionToThrow = new InvalidOperationException("Erreur réseau");
        var vm = CreateViewModel();

        await vm.LoadAsync();

        Assert.Single(_dialogService.ShownErrors);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task CardRows_PairsCardsTwoByTwo()
    {
        _statsService.ResultToReturn = CreateSampleData();
        var vm = CreateViewModel();

        await vm.InitializeAsync();

        Assert.NotEmpty(vm.CardRows);
        Assert.NotNull(vm.CardRows[0].Left);
        Assert.Empty(_dialogService.ShownErrors);
    }
}
