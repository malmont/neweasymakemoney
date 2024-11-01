namespace Easymakemoney.Models
{
    public class OrderCountServices: IOrderCountServices
    {
        private const string OrderCoutServicessUrl = "https://backend-strapi.online/jeesign/api";

        private readonly IHttpService _httpService;

        public OrderCountServices(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<OrderStatistics> GetOrderStatistics()
        {
            var url = $"{OrderCoutServicessUrl}/statistiques/nombre-commandes";
            var orderStatistics = await _httpService.GetAsync<OrderStatistics>(url);
            return orderStatistics ?? null;
        }

       
    }
}