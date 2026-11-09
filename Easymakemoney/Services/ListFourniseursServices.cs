namespace Easymakemoney.Services
{
    public class ListFourniseursServices : IListFourniseursServices
    {
        private readonly IHttpService _httpService;

        public ListFourniseursServices(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<ObservableCollection<ListFournisseur>> GetFourniseursList()
        {
            var url = "fournisseurs";
            var fourniseurs = await _httpService.GetAsync<ObservableCollection<ListFournisseur>>(url);
            return fourniseurs ?? new ObservableCollection<ListFournisseur>();
        }
        public async Task<bool> PostFourniseurs(ListFournisseur newFourniseurs)
        {
            var url = "fournisseurs";
            await _httpService.PostAsyncWithAuth<object>(url, newFourniseurs);
            return true;
        }

        public async Task<ObservableCollection<TypeFournisseur>> GetTypeFournisseurs()
        {
            var url = "type-fournisseurs";
            var typeFournisseurs = await _httpService.GetAsync<ObservableCollection<TypeFournisseur>>(url);
            return typeFournisseurs ?? new ObservableCollection<TypeFournisseur>();
        }

        public async Task<bool> DeleteFourniseurs(int id)
        {
            var url = $"fournisseurs/{id}";
            var result = await _httpService.DeleAsyncWithAuth<object>(url);
            return result;
        }
    }
}