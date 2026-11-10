using Newtonsoft.Json.Linq;

namespace Easymakemoney.Services
{
    public class CommandeReceptionService : ICommandeReceptionService
    {
        private readonly IHttpService _httpService;

        public CommandeReceptionService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<CommandeReception> ReceiveAsync(int commandeId, JObject? body = null) =>
            CommandeReception.Read(await _httpService.PostAsyncWithAuth<JObject?>($"commandes/{commandeId}/reception", body!));
    }
}
