namespace Easymakemoney.Services
{
    public class ListTransporteurService: IListTransporteurService
    {
        private const string transporteurtUrl = Configurations.BackendSymfonyUrl;
         private readonly IHttpService _httpService;
        public ListTransporteurService(IHttpService httpService)
        {
            _httpService = httpService;
        }
        public async Task<ObservableCollection<Transporteur>> GetListTransporteur()
        {
            var url = $"{transporteurtUrl}/transporteurs";
            var transporteurs = await _httpService.GetAsync<ObservableCollection<Transporteur>>(url);
            return transporteurs ?? new ObservableCollection<Transporteur>();
        }
       
      
    }
    
}