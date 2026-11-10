namespace Easymakemoney.Services
{
    /// <summary>Lignes d'achat à recevoir, toutes commandes (lot backend R1).</summary>
    public interface IToReceiveService
    {
        Task<List<ToReceiveLine>> GetLinesAsync(int limit = 200);
    }

    public class ToReceiveService : IToReceiveService
    {
        private readonly IHttpService _httpService;

        public ToReceiveService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<List<ToReceiveLine>> GetLinesAsync(int limit = 200) =>
            await _httpService.GetAsync<List<ToReceiveLine>>($"commandes/lignes/a-recevoir?page=1&limit={limit}") ?? new();
    }
}
