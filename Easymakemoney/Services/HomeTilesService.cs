namespace Easymakemoney.Services
{
    public class HomeTilesService : IHomeTilesService
    {
        private readonly IHttpService _httpService;

        public HomeTilesService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<List<HomeTile>> GetTilesAsync(string locale) =>
            await _httpService.GetAsync<List<HomeTile>>($"gestion/accueil/vignettes?locale={Uri.EscapeDataString(locale)}") ?? new();

        public async Task DismissAsync(string tileId) =>
            await _httpService.PostAsyncWithAuth<object>($"gestion/accueil/vignettes/{Uri.EscapeDataString(tileId)}/ecarter", null!);
    }
}
