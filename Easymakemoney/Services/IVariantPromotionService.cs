namespace Easymakemoney.Services
{
    public interface IVariantPromotionService
    {
        Task<VariantPromotionPreview> PreviewAsync(VariantPromotionRequest request);
        Task<VariantPromotionApplyResult> ApplyAsync(VariantPromotionRequest request, string previewHash, bool allowBelowCost, bool allowBigChange, bool allowReplace, bool allowAboveProductPromotion);
        Task EndAsync(int batchId);
        Task<List<VariantPromotionItem>> GetPromotionsAsync(string status = "all");
    }

    /// <summary>Promotions par variante (lot backend VAR) : aperçu sans écriture, application, fin d'un lot, liste.</summary>
    public class VariantPromotionService : IVariantPromotionService
    {
        private readonly IHttpService _httpService;

        public VariantPromotionService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<VariantPromotionPreview> PreviewAsync(VariantPromotionRequest request) =>
            await _httpService.PostAsyncWithAuth<VariantPromotionPreview>("gestion/promotions/variantes/apercu", request.ToBody()) ?? new VariantPromotionPreview();

        public async Task<VariantPromotionApplyResult> ApplyAsync(VariantPromotionRequest request, string previewHash, bool allowBelowCost, bool allowBigChange, bool allowReplace, bool allowAboveProductPromotion) =>
            await _httpService.PostAsyncWithAuth<VariantPromotionApplyResult>("gestion/promotions/variantes/appliquer",
                request.ToApplyBody(previewHash, allowBelowCost, allowBigChange, allowReplace, allowAboveProductPromotion)) ?? new VariantPromotionApplyResult();

        public async Task EndAsync(int batchId) =>
            await _httpService.PostAsyncWithAuth<object>("gestion/promotions/variantes/terminer", new Dictionary<string, object> { ["batchId"] = batchId });

        public async Task<List<VariantPromotionItem>> GetPromotionsAsync(string status = "all") =>
            await _httpService.GetAsync<List<VariantPromotionItem>>($"gestion/promotions/variantes?status={status}") ?? new List<VariantPromotionItem>();
    }
}
