namespace Easymakemoney.Services
{
    public interface IPromotionService
    {
        Task<PromotionPreview> PreviewAsync(PromotionRequest request);
        Task<PriceApplyResult> ApplyAsync(PromotionRequest request, string previewHash, bool allowBelowCost, bool allowBigChange, bool allowReplace);
        Task<List<PromotionItem>> GetPromotionsAsync(string status = "all");
        Task<PromotionEndResult> EndAsync(int batchId, bool onlyUntouched = false);
    }

    /// <summary>Promotion datée : périmètre (un seul), pourcentage, arrondi vers le bas, jours du magasin.</summary>
    public sealed class PromotionRequest
    {
        public List<int>? ProductIds { get; init; }
        public int? CommandeId { get; init; }
        public int? AdviceId { get; init; }
        public double Percent { get; init; }
        public string Rounding { get; init; } = "none";
        public DateTime StartDate { get; init; } = DateTime.Today;
        public DateTime EndDate { get; init; } = DateTime.Today.AddDays(14);

        public Dictionary<string, object> ToBody()
        {
            var body = new Dictionary<string, object>
            {
                ["percent"] = Percent,
                ["rounding"] = Rounding,
                ["startDate"] = TreasuryDates.Iso(StartDate),
                ["endDate"] = TreasuryDates.Iso(EndDate),
            };
            if (AdviceId is > 0)
                body["adviceId"] = AdviceId.Value;
            else if (CommandeId is > 0)
                body["commandeId"] = CommandeId.Value;
            else if (ProductIds is { Count: > 0 })
                body["productIds"] = ProductIds;
            return body;
        }
    }

    public class PromotionService : IPromotionService
    {
        private readonly IHttpService _httpService;

        public PromotionService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public Task<PromotionPreview> PreviewAsync(PromotionRequest request) =>
            _httpService.PostAsyncWithAuth<PromotionPreview>("gestion/promotions/apercu", request.ToBody());

        public Task<PriceApplyResult> ApplyAsync(PromotionRequest request, string previewHash, bool allowBelowCost, bool allowBigChange, bool allowReplace)
        {
            var body = request.ToBody();
            body["previewHash"] = previewHash;
            body["allowBelowCost"] = allowBelowCost;
            body["allowBigChange"] = allowBigChange;
            body["allowReplace"] = allowReplace;
            return _httpService.PostAsyncWithAuth<PriceApplyResult>("gestion/promotions/appliquer", body);
        }

        public async Task<List<PromotionItem>> GetPromotionsAsync(string status = "all") =>
            await _httpService.GetAsync<List<PromotionItem>>($"gestion/promotions?status={status}") ?? new();

        public Task<PromotionEndResult> EndAsync(int batchId, bool onlyUntouched = false) =>
            _httpService.PostAsyncWithAuth<PromotionEndResult>("gestion/promotions/terminer", new Dictionary<string, object>
            {
                ["batchId"] = batchId,
                ["onlyUntouched"] = onlyUntouched,
            });
    }
}
