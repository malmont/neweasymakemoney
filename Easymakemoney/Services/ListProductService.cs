namespace Easymakemoney.Services;

public class ListProductService : IListProductService
{
    private const string ProductsUrl = "https://backend-strapi.online/jeesign/api";
    private readonly IHttpService _httpService;

    public ListProductService(IHttpService httpService)
    {
        _httpService = httpService;
    }

    public async Task<ObservableCollection<ListProduct>> GetProductsList(int id)
    {
        var url = $"{ProductsUrl}/commandes/{id}/products";
        var products = await _httpService.GetAsync<ObservableCollection<ListProduct>>(url);
        return products ?? new ObservableCollection<ListProduct>();
    }
    public async Task<bool> PostProducts(ListProduct newProducts, int id)
    {
        var url = $"{ProductsUrl}/commandes/{id}/products";
        var result = await _httpService.PostAsyncWithAuth<object>(url, newProducts);
        return result != null;
    }

    public async Task<bool> DeleteProducts(int id)
    {
        var url = $"{ProductsUrl}/products/{id}";
        var result = await _httpService.DeleAsyncWithAuth<object>(url);
        return result != null;
    }
}