namespace Easymakemoney.Services
{
    public interface IYearStatisticsService
    {
        /// <summary>L'exercice (année de début) ; null : l'exercice en cours.</summary>
        Task<YearStatistics> GetAsync(int? year, int? pricePercent);
        /// <summary>Avis de l'IA sur un exercice terminé (409 YEAR_NOT_FINISHED sinon).</summary>
        Task<YearComment> GetCommentAsync(int year);
        Task<FiscalYearSetting> GetFiscalYearAsync();
        Task<FiscalYearSetting> SaveFiscalYearAsync(int startMonth);
    }
}
