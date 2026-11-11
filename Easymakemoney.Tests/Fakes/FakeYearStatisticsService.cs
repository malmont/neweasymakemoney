using Easymakemoney.Models;
using Easymakemoney.Services;

namespace Easymakemoney.Tests.Fakes;

public class FakeYearStatisticsService : IYearStatisticsService
{
    public List<string> RequestedUrls { get; } = new();
    public List<(int? Year, int? PricePercent)> GetCalls { get; } = new();
    public List<int> CommentCalls { get; } = new();
    public List<int> SaveFiscalYearCalls { get; } = new();

    public YearStatistics ResultToReturn { get; set; } = new();
    public YearComment CommentToReturn { get; set; } = new();
    public FiscalYearSetting FiscalYearToReturn { get; set; } = new();

    public Exception? ExceptionToThrow { get; set; }
    public Exception? CommentExceptionToThrow { get; set; }
    public Exception? SaveExceptionToThrow { get; set; }

    public Task<YearStatistics> GetAsync(int? year, int? pricePercent)
    {
        GetCalls.Add((year, pricePercent));
        RequestedUrls.Add(YearStatisticsService.YearUrl(year, pricePercent));
        if (ExceptionToThrow != null)
            throw ExceptionToThrow;
        return Task.FromResult(ResultToReturn);
    }

    public Task<YearComment> GetCommentAsync(int year)
    {
        CommentCalls.Add(year);
        RequestedUrls.Add($"gestion/statistiques/annee/avis?year={year}&locale=fr");
        if (CommentExceptionToThrow != null)
            throw CommentExceptionToThrow;
        return Task.FromResult(CommentToReturn);
    }

    public Task<FiscalYearSetting> GetFiscalYearAsync()
    {
        RequestedUrls.Add("gestion/reglages/exercice");
        return Task.FromResult(FiscalYearToReturn);
    }

    public Task<FiscalYearSetting> SaveFiscalYearAsync(int startMonth)
    {
        SaveFiscalYearCalls.Add(startMonth);
        RequestedUrls.Add("gestion/reglages/exercice");
        if (SaveExceptionToThrow != null)
            throw SaveExceptionToThrow;
        FiscalYearToReturn = new FiscalYearSetting { startMonth = startMonth };
        return Task.FromResult(FiscalYearToReturn);
    }
}

