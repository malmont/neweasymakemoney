namespace Easymakemoney.Services
{
    public class ListCommandService : IListCommandService
    {
        private const string CommandesUrl = "https://backend-strapi.online/jeesign/api";
        private readonly IHttpService _httpService;

        public ListCommandService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<ObservableCollection<ListCommand>> GetCommandesList(int id)
        {
            var url = $"{CommandesUrl}/collections/{id}/commandes";
            var commandes = await _httpService.GetAsync<ObservableCollection<ListCommand>>(url);
            return commandes ?? new ObservableCollection<ListCommand>();
        }
        public async Task<bool> PostCommandes(ListCommand newCommandes, int id)
        {
            var url = $"{CommandesUrl}/collections/{id}/commandes";
            var result = await _httpService.PostAsyncWithAuth<object>(url, newCommandes);
            return result != null;
        }

        public async Task<bool> DeleteCommandes(int id)
        {
            var url = $"{CommandesUrl}/commandes/{id}";
            var result = await _httpService.DeleAsyncWithAuth<object>(url);
            return result != null;
        }
    }
}
