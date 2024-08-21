namespace Easymakemoney.Services
{
    public class ListFourniseursServices : IListFourniseursServices
    {
        private const string FourniseursUrl = "https://backend-strapi.online/jeesign/api";
        private readonly IHttpService _httpService;

        public ListFourniseursServices(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<ObservableCollection<ListFournisseur>> GetFourniseursList()
        {
            var url = $"{FourniseursUrl}/fournisseurs";
            var fourniseurs = await _httpService.GetAsync<ObservableCollection<ListFournisseur>>(url);
            return fourniseurs ?? new ObservableCollection<ListFournisseur>();
        }
        public async Task<bool> PostFourniseurs(ListFournisseur newFourniseurs)
        {
            var url = $"{FourniseursUrl}/fournisseurs";
            var result = await _httpService.PostAsyncWithAuth<object>(url, newFourniseurs);
            return result != null;
        }

        public async Task<bool> DeleteFourniseurs(int id)
        {
            var url = $"{FourniseursUrl}/fournisseurs/{id}";
            var result = await _httpService.DeleAsyncWithAuth<object>(url);
            return result != null;
        }
    }
}