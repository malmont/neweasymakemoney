namespace Easymakemoney.Services
{
    public class ListSizesServices:IListSizesServices
    {
        private const string SizesUrl =Configurations.BackendSymfonyUrl;
        private readonly IHttpService _httpService;

        public ListSizesServices(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<ObservableCollection<SizeProduct>> GetSizesList()
        {
            var sizes = await _httpService.GetAsync<ObservableCollection<SizeProduct>>(SizesUrl +"/sizes");
            return sizes ?? new ObservableCollection<SizeProduct>();
        }
   
    }
}