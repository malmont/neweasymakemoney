namespace Easymakemoney.Services
{
    public class ListStyleService : IListStyleService
    {
         private readonly IHttpService _httpService;

        public ListStyleService(IHttpService httpService)
        {
           _httpService = httpService;

        }

        public async Task<ObservableCollection<Styles>> GetStyleList()
        {
            var categories = await _httpService.GetAsync<ObservableCollection<Styles>>("styles");
            return categories ?? new ObservableCollection<Styles>();
        }
    }
}