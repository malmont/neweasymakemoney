namespace Easymakemoney.Services;

public class ListProductService : IListProductService
{
    private readonly IHttpService _httpService;

    public ListProductService(IHttpService httpService)
    {
        _httpService = httpService;
    }

    public async Task<ObservableCollection<ListProduct>> GetProductsList(int id)
    {
        var url = $"commandes/{id}/products";
        var products = await _httpService.GetAsync<ObservableCollection<ListProduct>>(url);
        return products ?? new ObservableCollection<ListProduct>();
    }
    public async Task<bool> PostProducts(ListProduct newProduct, Stream imageStream, string fileName, int id)
    {
        var url = $"commandes/{id}/products";

        // Utilisez la méthode `PostMultipartAsyncWithAuth` pour envoyer le produit et l'image
        var result = await _httpService.PostMultipartAsyncWithAuth<object>(url, newProduct, imageStream, fileName);

        return result != null;
    }

    public async Task<bool> DeleteProducts(int id)
    {
        var url = $"products/{id}";
        var result = await _httpService.DeleAsyncWithAuth<object>(url);
        return result != null;
    }
}