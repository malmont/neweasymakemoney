namespace Easymakemoney.Services
{
    public interface IAverageOrderValueServices
    {
        Task<AverageOrderValueStatistics> GetAverageOrderValueStatistics();
    }

}