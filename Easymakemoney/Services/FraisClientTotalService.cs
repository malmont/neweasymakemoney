namespace Easymakemoney.Services
{
    public class FraisClientTotalService : IFraisClientTotalService
    {
        private const string FraisClientTotalUrl = Configurations.BackendSymfonyUrl;
        private readonly IHttpService _httpService;

        public FraisClientTotalService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<CarrierStatisticsResponse> GetFraisClientCarrierAsync()
        {
            var url = $"{FraisClientTotalUrl}/carrier/statistics";
            var monthlyCarrierData = await _httpService.GetAsync<CarrierStatisticsResponse>(url);
            return monthlyCarrierData ?? null;

        }

        public async Task<TaxStatisticsResponse> GetFraisClientTaxAsync()
        {
            var url = $"{FraisClientTotalUrl}/taxes/monthly";
            var monthlyTaxData = await _httpService.GetAsync<TaxStatisticsResponse>(url);
            return monthlyTaxData ?? null;

        }
    }
}