using System.Globalization;
using Newtonsoft.Json.Linq;

namespace Easymakemoney.Services
{
    public class CampaignService : ICampaignService
    {
        private const string BaseUrl = "gestion/campagnes";
        private readonly IHttpService _httpService;

        public CampaignService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<List<Campaign>> ListAsync(int page, int limit) =>
            await _httpService.GetAsync<List<Campaign>>(
                $"{BaseUrl}?page={page.ToString(CultureInfo.InvariantCulture)}&limit={limit.ToString(CultureInfo.InvariantCulture)}")
            ?? new List<Campaign>();

        public Task<Campaign> GetAsync(int id) =>
            _httpService.GetAsync<Campaign>($"{BaseUrl}/{id}");

        public Task<Campaign> CreateAsync(JObject body) =>
            _httpService.PostAsyncWithAuth<Campaign>(BaseUrl, body);

        public Task<Campaign> UpdateAsync(int id, JObject body) =>
            _httpService.PatchAsyncWithAuth<Campaign>($"{BaseUrl}/{id}", body);

        public async Task DeleteAsync(int id) =>
            await _httpService.DeleAsyncWithAuth<object>($"{BaseUrl}/{id}");

        public Task<CampaignProfitability> GetProfitabilityAsync(int id) =>
            _httpService.GetAsync<CampaignProfitability>($"{BaseUrl}/{id}/rentabilite");
    }
}
