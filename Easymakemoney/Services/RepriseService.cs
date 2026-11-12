namespace Easymakemoney.Services
{
    public class RepriseService : IRepriseService
    {
        private readonly IHttpService _httpService;

        public RepriseService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public Task<RepriseState> GetStateAsync() =>
            _httpService.GetAsync<RepriseState>("gestion/reprise");

        public Task<RepriseSimulation> SimulateAsync(int months, double? coefficient, int page, int limit)
        {
            var body = BaseBody(months, coefficient);
            body["page"] = page;
            body["limit"] = limit;
            return _httpService.PostAsyncWithAuth<RepriseSimulation>("gestion/reprise/simulation", body);
        }

        public async Task ApplyAsync(int months, double? coefficient, IEnumerable<RepriseProduct> editedProducts)
        {
            var body = BaseBody(months, coefficient);
            body["costs"] = editedProducts
                .Select(product => new { productId = product.productId, purchasePrice = product.EditedCostCents!.Value })
                .ToList();
            await _httpService.PostAsyncWithAuth<RepriseState>("gestion/reprise/appliquer", body);
        }

        public async Task CancelAsync() =>
            await _httpService.PostAsyncWithAuth<object>("gestion/reprise/annuler", null!);

        private static Dictionary<string, object> BaseBody(int months, double? coefficient)
        {
            var body = new Dictionary<string, object> { ["months"] = months };
            if (coefficient.HasValue)
            {
                body["estimateCoefficient"] = coefficient.Value;
            }
            return body;
        }
    }
}
