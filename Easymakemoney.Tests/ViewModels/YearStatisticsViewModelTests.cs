using System.Globalization;
using System.Net;
using Easymakemoney.Models;
using Easymakemoney.Models.Api;
using Easymakemoney.Services.Api;
using Easymakemoney.Tests.Fakes;
using Easymakemoney.Utilities;
using Easymakemoney.ViewModels.Statistiques;
using Xunit;

namespace Easymakemoney.Tests.ViewModels;

[Collection("SiteCurrency")]
public class YearStatisticsViewModelTests : IDisposable
{
    private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;
    private readonly FakeYearStatisticsService _yearService = new();
    private readonly FakeDialogService _dialogService = new();

    public YearStatisticsViewModelTests()
    {
        CultureInfo.CurrentCulture = new CultureInfo("fr-CA");
        SiteCurrency.Reset();
        YearStatisticsViewModel.FiscalYearChanged = false;
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _originalCulture;
        SiteCurrency.Reset();
        YearStatisticsViewModel.FiscalYearChanged = false;
    }

    private YearStatisticsViewModel CreateViewModel() =>
        new(_yearService, _dialogService);

    private static YearStatistics CreateSampleYear(int year, bool finished = false)
    {
        return new YearStatistics
        {
            year = year,
            fiscalYear = new FiscalYearInfo { startMonth = 1, label = $"Exercice {year}" },
            currency = "CAD",
            status = finished ? "finished" : "in_progress",
            totals = new YearMonth
            {
                sales = 100000,
                grossMargin = 40000,
                fixedCosts = 10000,
                result = 10000
            },
            months = new List<YearMonth>
            {
                new()
                {
                    month = 1,
                    from = $"{year}-01-01",
                    to = $"{year}-01-31",
                    result = 1000,
                    sales = 5000,
                    grossMargin = 2000
                }
            },
            levers = new List<YearLever>
            {
                new() { key = "price", label = "Prix +5 %", effect = 5000 }
            }
        };
    }

    [Fact]
    public async Task InitializeAsync_FirstLoad_RequestsWithoutYear()
    {
        _yearService.ResultToReturn = CreateSampleYear(2026, finished: false);
        var vm = CreateViewModel();

        await vm.InitializeAsync();
        await vm.InitializeAsync(); // Second appel : ne recharge pas

        var url = Assert.Single(_yearService.RequestedUrls);
        Assert.Equal("gestion/statistiques/annee?locale=fr", url);
        Assert.True(vm.CanGoBack);
        Assert.False(vm.CanGoForward);
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task PreviousYear_And_NextYear_NavigateWithBounds()
    {
        _yearService.ResultToReturn = CreateSampleYear(2026, finished: false);
        var vm = CreateViewModel();
        await vm.InitializeAsync();

        // 1. Reculer d'un an (2025)
        _yearService.ResultToReturn = CreateSampleYear(2025, finished: true);
        await vm.PreviousYearCommand.ExecuteAsync(null);

        Assert.Equal("gestion/statistiques/annee?year=2025&locale=fr", _yearService.RequestedUrls.Last());
        Assert.True(vm.CanGoBack);
        Assert.True(vm.CanGoForward);

        // 2. Avancer d'un an (retour à 2026)
        _yearService.ResultToReturn = CreateSampleYear(2026, finished: false);
        await vm.NextYearCommand.ExecuteAsync(null);

        Assert.Equal("gestion/statistiques/annee?year=2026&locale=fr", _yearService.RequestedUrls.Last());
        Assert.False(vm.CanGoForward);

        // 3. Borne supérieure : NextYear ne doit rien faire sur l'année en cours
        var countBeforeNext = _yearService.RequestedUrls.Count;
        await vm.NextYearCommand.ExecuteAsync(null);
        Assert.Equal(countBeforeNext, _yearService.RequestedUrls.Count);

        // 4. Borne inférieure : sur l'année 2000, PreviousYear ne doit rien faire
        _yearService.ResultToReturn = CreateSampleYear(2000, finished: true);
        await vm.PreviousYearCommand.ExecuteAsync(null);
        Assert.False(vm.CanGoBack);

        var countBeforePrev = _yearService.RequestedUrls.Count;
        await vm.PreviousYearCommand.ExecuteAsync(null);
        Assert.Equal(countBeforePrev, _yearService.RequestedUrls.Count);

        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task SelectPrice_SendsPricePercent_AndKeepsDisplayedYear()
    {
        _yearService.ResultToReturn = CreateSampleYear(2026, finished: false);
        var vm = CreateViewModel();
        await vm.InitializeAsync();

        _yearService.ResultToReturn = CreateSampleYear(2025, finished: true);
        await vm.PreviousYearCommand.ExecuteAsync(null);

        await vm.SelectPriceCommand.ExecuteAsync("10");

        Assert.Equal("gestion/statistiques/annee?year=2025&pricePercent=10&locale=fr", _yearService.RequestedUrls.Last());
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task AskComment_OnlyForFinishedYear_SendsPostToContractRoute()
    {
        var vm = CreateViewModel();

        // 1. Sur un exercice en cours : aucune requête
        _yearService.ResultToReturn = CreateSampleYear(2026, finished: false);
        await vm.InitializeAsync();

        await vm.AskCommentCommand.ExecuteAsync(null);
        Assert.Empty(_yearService.CommentCalls);

        // 2. Sur un exercice terminé : requête envoyée
        _yearService.ResultToReturn = CreateSampleYear(2025, finished: true);
        _yearService.CommentToReturn = new YearComment
        {
            year = 2025,
            comment = new YearCommentBody { title = "Bilan 2025", text = "Bonne année" }
        };
        await vm.PreviousYearCommand.ExecuteAsync(null);

        await vm.AskCommentCommand.ExecuteAsync(null);

        Assert.Single(_yearService.CommentCalls);
        Assert.Equal(2025, _yearService.CommentCalls[0]);
        Assert.Contains("gestion/statistiques/annee/avis?year=2025&locale=fr", _yearService.RequestedUrls);
        Assert.True(vm.HasComment);
        Assert.NotNull(vm.Comment);
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task AskComment_429CommentLimit_ShowsInfoDialogInsteadOfError()
    {
        _yearService.ResultToReturn = CreateSampleYear(2025, finished: true);
        var apiError = ApiError.FromHttpResponse("""{ "error": "COMMENT_LIMIT" }""", (HttpStatusCode)429, null);
        _yearService.CommentExceptionToThrow = new ApiException(apiError, (HttpStatusCode)429);

        var vm = CreateViewModel();
        await vm.InitializeAsync();

        await vm.AskCommentCommand.ExecuteAsync(null);

        Assert.Contains("Le guide a déjà donné 10 avis aujourd'hui. Réessayez demain.", _dialogService.ShownInfos);
        Assert.Empty(_dialogService.ShownErrors);
        Assert.False(vm.IsCommentBusy);
    }

    [Fact]
    public async Task FiscalYearChanged_TriggersReloadOfCurrentYear_AndResetsFlag()
    {
        _yearService.ResultToReturn = CreateSampleYear(2026, finished: false);
        var vm = CreateViewModel();
        await vm.InitializeAsync();
        Assert.Single(_yearService.RequestedUrls);

        YearStatisticsViewModel.FiscalYearChanged = true;

        await vm.InitializeAsync();

        Assert.Equal(2, _yearService.RequestedUrls.Count);
        Assert.Equal("gestion/statistiques/annee?locale=fr", _yearService.RequestedUrls.Last());
        Assert.False(YearStatisticsViewModel.FiscalYearChanged);
        Assert.Empty(_dialogService.ShownErrors);
    }

    [Fact]
    public async Task FinishedYear_HidesLevers()
    {
        // Exercice terminé
        _yearService.ResultToReturn = CreateSampleYear(2025, finished: true);
        var vm = CreateViewModel();
        await vm.InitializeAsync();

        Assert.Empty(vm.Levers);
        Assert.False(vm.HasLevers);

        // Exercice en cours
        _yearService.ResultToReturn = CreateSampleYear(2026, finished: false);
        var vmCurrent = CreateViewModel();
        await vmCurrent.InitializeAsync();

        Assert.NotEmpty(vmCurrent.Levers);
        Assert.True(vmCurrent.HasLevers);
        Assert.Empty(_dialogService.ShownErrors);
    }
}
