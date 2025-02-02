namespace Easymakemoney.Services
{
  public class ListCategoryServices : IListCategoryServices
  {
    private const string CategoriesUrl = Configurations.BackendSymfonyUrl;
    private readonly IHttpService _httpService;

    public ListCategoryServices(IHttpService httpService)
    {
      _httpService = httpService;
    }

    public async Task<ObservableCollection<Category>> GetCategoryList()
    {
      var categories = await _httpService.GetAsync<ObservableCollection<Category>>(CategoriesUrl + "/category");
      return categories ?? new ObservableCollection<Category>();
    }
  }
}