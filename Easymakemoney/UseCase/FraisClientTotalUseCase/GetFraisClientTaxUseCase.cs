namespace Easymakemoney.UseCase.FraisClientTotalUseCase
{
    public class GetFraisClientTaxUseCase
    {
        private readonly IFraisClientTotalService _fraisClientTotalService;

        public GetFraisClientTaxUseCase(IFraisClientTotalService fraisClientTotalService)
        {
            _fraisClientTotalService = fraisClientTotalService;
        }

        public async Task<TaxStatisticsResponse> Execute()
        {
            return await _fraisClientTotalService.GetFraisClientTaxAsync();
        }
    }
}