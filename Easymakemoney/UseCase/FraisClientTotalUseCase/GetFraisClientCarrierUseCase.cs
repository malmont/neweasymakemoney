namespace Easymakemoney.UseCase.FraisClientTotalUseCase
{
    public class GetFraisClientCarrierUseCase
    {
        private readonly IFraisClientTotalService _fraisClientTotalService;

        public GetFraisClientCarrierUseCase(IFraisClientTotalService fraisClientTotalService)
        {
            _fraisClientTotalService = fraisClientTotalService;
        }

        public async Task<CarrierStatisticsResponse>Execute()
        {
            return await _fraisClientTotalService.GetFraisClientCarrierAsync();
        }
    }
}