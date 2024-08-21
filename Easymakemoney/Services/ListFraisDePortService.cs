namespace Easymakemoney.Services
{
    public class ListFraisDePortService: IListFraisDePortService
    {
        private const string fraisDePortUrl = "https://backend-strapi.online/jeesign/api";
        private readonly IHttpService _httpService;
        public ListFraisDePortService(IHttpService httpService)
        {
            _httpService = httpService;
        }
        public async Task<FraisDePort> GetListFraisDePort(int commandId)
        {
            var url = $"{fraisDePortUrl}/commandes/{commandId}/frais-de-port";
            var fraisDePort = await _httpService.GetAsync<FraisDePort>(url);
            return fraisDePort ;
        }
        public async Task<bool> PostFraisDePort(FraisDePort newFraisDePort, int commandId)
        {
            var url = $"{fraisDePortUrl}/commandes/{commandId}/frais-de-port";
            var result = await _httpService.PostAsyncWithAuth<object>(url, newFraisDePort);
            return result != null;
        }
        public async Task<bool> DeleteFraisDePort(int id)
        {
            var url = $"{fraisDePortUrl}/frais-de-port/{id}";
            var result = await _httpService.DeleAsyncWithAuth<object>(url);
            return result != null;
        }
       
    }
}