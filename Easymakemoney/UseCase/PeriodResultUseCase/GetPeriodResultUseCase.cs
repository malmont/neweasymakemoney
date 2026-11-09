namespace Easymakemoney.UseCase.PeriodResultUseCase
{
    public class GetPeriodResultUseCase
    {
        private readonly IPeriodResultService _service;

        public GetPeriodResultUseCase(IPeriodResultService service)
        {
            _service = service;
        }

        public Task<PeriodResult> ExecuteAsync(int periodId) => _service.GetResultAsync(periodId);
    }
}
