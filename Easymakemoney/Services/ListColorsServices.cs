namespace Easymakemoney.Services
{
    public class ListColorsServices: IListColorsServices
    {
        private readonly IHttpService _httpService;

        public ListColorsServices(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<ObservableCollection<ColorsProduct>> GetColorsList()
        {
            var colors = await _httpService.GetAsync<ObservableCollection<ColorsProduct>>("colors");
            return colors ?? new ObservableCollection<ColorsProduct>();
        }

    
     
    }
}