using System.Net;
using Easymakemoney.Services.Api;

namespace Easymakemoney.Services
{
    public class ListFraisDePortService: IListFraisDePortService
    {
        private readonly IHttpService _httpService;
        public ListFraisDePortService(IHttpService httpService)
        {
            _httpService = httpService;
        }
        public async Task<FraisDePort?> GetListFraisDePort(int commandId)
        {
            var url = $"commandes/{commandId}/frais-de-port";
            try
            {
                var fraisDePort = await _httpService.GetAsync<FraisDePort>(url);
                return fraisDePort;
            }
            catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Le backend rend 404 lorsqu'il n'y a aucun frais de port pour cette commande (normal)
                return null;
            }
        }
        public async Task<bool> PostFraisDePort(FraisDePort newFraisDePort, int commandId)
        {
            var url = $"commandes/{commandId}/frais-de-port";
            await _httpService.PostAsyncWithAuth<object>(url, newFraisDePort);
            return true;
        }
        public async Task<bool> DeleteFraisDePort(int commandId)
        {
            var url = $"commandes/{commandId}/frais-de-port";
            var result = await _httpService.DeleAsyncWithAuth<object>(url);
            return result;
        }
       
    }
}