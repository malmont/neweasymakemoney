namespace Easymakemoney.Services
{
    public class PeriodResultService : IPeriodResultService
    {
        private readonly IHttpService _httpService;

        public PeriodResultService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public Task<PeriodResult> GetResultAsync(int periodId) =>
            _httpService.GetAsync<PeriodResult>($"periodes/{periodId}/resultat");
    }
}
