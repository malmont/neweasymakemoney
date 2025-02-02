namespace Easymakemoney.Models

{
    public class AverageOrderValueServices : IAverageOrderValueServices
    {
        private const string AverageOrderValueStatisticsUrl = Configurations.BackendSymfonyUrl;

        private readonly IHttpService _httpService;

        public AverageOrderValueServices(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<AverageOrderValueStatistics> GetAverageOrderValueStatistics()
        {
            var url = $"{AverageOrderValueStatisticsUrl}/statistiques/panier-moyen";
            var averageOrderValueStatistics = await _httpService.GetAsync<AverageOrderValueStatistics>(url);
            return averageOrderValueStatistics ?? null;
        }

       
    }
}