namespace Easymakemoney.Services
{
    public interface IRentalPromotionService
    {
        Task<RentalPromotionPreview> PreviewAsync(RentalPromotionRequest request);
        Task<RentalPromotionApplyResult> ApplyAsync(RentalPromotionRequest request, string previewHash);
        Task EndAsync(int batchId);
        Task<List<RentalPromotionItem>> GetPromotionsAsync();
    }

    /// <summary>Promotions de location (lot backend LOC3) : aperçu sans écriture, application tout ou rien, fin d'un lot.</summary>
    public class RentalPromotionService : IRentalPromotionService
    {
        private readonly IHttpService _httpService;

        public RentalPromotionService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<RentalPromotionPreview> PreviewAsync(RentalPromotionRequest request) =>
            await _httpService.PostAsyncWithAuth<RentalPromotionPreview>("gestion/locations/promotions/apercu", request.ToBody()) ?? new RentalPromotionPreview();

        public async Task<RentalPromotionApplyResult> ApplyAsync(RentalPromotionRequest request, string previewHash) =>
            await _httpService.PostAsyncWithAuth<RentalPromotionApplyResult>("gestion/locations/promotions/appliquer", request.ToBody(previewHash)) ?? new RentalPromotionApplyResult();

        public async Task EndAsync(int batchId) =>
            await _httpService.PostAsyncWithAuth<object>("gestion/locations/promotions/terminer", new Dictionary<string, object> { ["batchId"] = batchId });

        public async Task<List<RentalPromotionItem>> GetPromotionsAsync() =>
            await _httpService.GetAsync<List<RentalPromotionItem>>("gestion/locations/promotions") ?? new List<RentalPromotionItem>();
    }
}
