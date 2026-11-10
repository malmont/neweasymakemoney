namespace Easymakemoney.UseCase.PeriodResultUseCase
{
    public class PeriodUseCase
    {
        private readonly IPeriodService _service;

        public PeriodUseCase(IPeriodService service)
        {
            _service = service;
        }

        public Task<ListCollection?> GetCurrentPeriodAsync() => _service.GetCurrentPeriodAsync();

        public Task UpdateBudgetAsync(int periodId, double amount) =>
            _service.UpdateBudgetAsync(periodId, Utilities.MoneyHelper.ToCents(amount));
    }
}
