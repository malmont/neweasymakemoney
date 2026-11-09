namespace Easymakemoney.Services
{
    public interface IPeriodResultService
    {
        Task<PeriodResult> GetResultAsync(int periodId);
    }
}
