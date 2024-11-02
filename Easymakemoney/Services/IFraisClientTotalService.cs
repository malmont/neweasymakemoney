namespace Easymakemoney.Services
{
    public interface IFraisClientTotalService
    {
        Task<CarrierStatisticsResponse>GetFraisClientCarrierAsync();
        Task<TaxStatisticsResponse> GetFraisClientTaxAsync();


    }
}