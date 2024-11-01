namespace Easymakemoney.UseCase
{
    public class AverageOrderValueUseCase
    {
        private readonly IAverageOrderValueServices _averageOrderValueServices;

        public AverageOrderValueUseCase(IAverageOrderValueServices averageOrderValueServices)
        {
            _averageOrderValueServices = averageOrderValueServices;
        }

        public async Task<AverageOrderValueStatistics> GetAverageOrderValueStatistics()
        {
            return await _averageOrderValueServices.GetAverageOrderValueStatistics();
        }
    }
}