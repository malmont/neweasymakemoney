namespace Easymakemoney.Services
{
    public class ListStyleService : IListStyleService
    {
         private readonly IHttpService _httpService;
        private const string Url = Configurations.BackendSymfonyUrl;

        public ListStyleService(IHttpService httpService)
        {
           _httpService = httpService;

        }

        public async Task<ObservableCollection<Styles>> GetStyleList()
        {
            var categories = await _httpService.GetAsync<ObservableCollection<Styles>>(Url + "/styles");
            return categories ?? new ObservableCollection<Styles>();
        }
    }
}