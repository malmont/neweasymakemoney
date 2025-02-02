namespace Easymakemoney.Services
{
    public class ListProductVariantService:IListProductVarianstService

    {
        private const string ProductVariantUrl = Configurations.BackendSymfonyUrl;
        private readonly IHttpService _httpService;

        public ListProductVariantService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<ObservableCollection<ProductVariant>> GetProductVariantList(int id)
        {
            var url = $"{ProductVariantUrl}/products/{id}/variants";
            var productVariants = await _httpService.GetAsync<ObservableCollection<ProductVariant>>(url);
            return productVariants ?? new ObservableCollection<ProductVariant>();
        }

        public async Task<bool> PostProductVariant(ProductVariant newProductVariant, int id)
        {
            var url = $"{ProductVariantUrl}/products/{id}/variants";
            var result = await _httpService.PostAsyncWithAuth<object>(url, newProductVariant);
            return result != null;
        }

        public async Task<bool> DeleteProductVariant(int id)
        {
            var url = $"{ProductVariantUrl}/product-variants/{id}";
            var result = await _httpService.DeleAsyncWithAuth<object>(url);
            return result != null;
        }
    }
}