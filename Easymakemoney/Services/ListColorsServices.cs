namespace Easymakemoney.Services
{
    public class ListColorsServices: IListColorsServices
    {
        private const string ColorsUrl = "https://backend-strapi.online/jeesign/api";
        private readonly IHttpService _httpService;

        public ListColorsServices(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<ObservableCollection<ColorsProduct>> GetColorsList()
        {
            var colors = await _httpService.GetAsync<ObservableCollection<ColorsProduct>>(ColorsUrl +"/colors");
            return colors ?? new ObservableCollection<ColorsProduct>();
        }

    
     
    }
}