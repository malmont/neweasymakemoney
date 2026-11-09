namespace Easymakemoney.Services
{
    public class ProductAttachmentService : IProductAttachmentService
    {
        private readonly IHttpService _httpService;

        public ProductAttachmentService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public Task<AvailableProductsPage> GetAvailableAsync(string? query, int page, int limit)
        {
            var q = Uri.EscapeDataString(query?.Trim() ?? string.Empty);
            return _httpService.GetAsync<AvailableProductsPage>($"commandes/products/disponibles?q={q}&page={page}&limit={limit}&locale=fr");
        }

        public async Task AttachAsync(int commandId, IEnumerable<AvailableProduct> products)
        {
            // purchasePrice n'est envoyé que s'il a été saisi : sinon le backend garde le coût de l'article.
            var lines = products.Select(product =>
            {
                var line = new Dictionary<string, object> { ["productId"] = product.id };
                if (product.EditedCostCents.HasValue)
                {
                    line["purchasePrice"] = product.EditedCostCents.Value;
                }
                return line;
            }).ToList();

            await _httpService.PostAsyncWithAuth<object>($"commandes/{commandId}/products/rattacher", new { products = lines });
        }

        public Task<bool> DetachAsync(int commandId, int productId) =>
            _httpService.DeleAsyncWithAuth<object>($"commandes/{commandId}/products/{productId}");
    }
}
