namespace Easymakemoney.Services
{
    public class PeriodService : IPeriodService
    {
        private readonly IHttpService _httpService;

        public PeriodService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public Task<ListCollection?> GetCurrentPeriodAsync() =>
            _httpService.GetAsync<ListCollection?>("periodes/courante");

        public async Task UpdateBudgetAsync(int periodId, long budgetCents) =>
            await _httpService.PatchAsyncWithAuth<object>($"periodes/{periodId}", new { budget = budgetCents });
    }
}
