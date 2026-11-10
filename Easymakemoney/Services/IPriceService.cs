namespace Easymakemoney.Services
{
    public interface IPriceService
    {
        Task<PricePreview> PreviewAsync(PriceChangeRequest request);
        Task<PriceApplyResult> ApplyAsync(PriceChangeRequest request, string previewHash, bool allowBelowCost, bool allowBigChange);
        Task<List<PriceBatch>> GetBatchesAsync(int limit = 20);
        Task<PriceUndoResult> UndoAsync(int batchId, bool onlyUntouched = false);
    }

    /// <summary>Périmètre (un seul : articles, commande ou conseil), mode et valeur d'un changement de prix.</summary>
    public sealed class PriceChangeRequest
    {
        public List<int>? ProductIds { get; init; }
        public int? CommandeId { get; init; }
        public int? AdviceId { get; init; }
        public string Mode { get; init; } = "percent";
        public double Value { get; init; }
        public string Rounding { get; init; } = "none";

        public Dictionary<string, object> ToBody()
        {
            var body = new Dictionary<string, object> { ["mode"] = Mode, ["value"] = Value, ["rounding"] = Rounding };
            if (AdviceId is > 0)
                body["adviceId"] = AdviceId.Value;
            else if (CommandeId is > 0)
                body["commandeId"] = CommandeId.Value;
            else if (ProductIds is { Count: > 0 })
                body["productIds"] = ProductIds;
            return body;
        }
    }

    public class PriceService : IPriceService
    {
        private readonly IHttpService _httpService;

        public PriceService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public Task<PricePreview> PreviewAsync(PriceChangeRequest request) =>
            _httpService.PostAsyncWithAuth<PricePreview>("gestion/prix/apercu", request.ToBody());

        public Task<PriceApplyResult> ApplyAsync(PriceChangeRequest request, string previewHash, bool allowBelowCost, bool allowBigChange)
        {
            var body = request.ToBody();
            body["previewHash"] = previewHash;
            body["allowBelowCost"] = allowBelowCost;
            body["allowBigChange"] = allowBigChange;
            return _httpService.PostAsyncWithAuth<PriceApplyResult>("gestion/prix/appliquer", body);
        }

        public async Task<List<PriceBatch>> GetBatchesAsync(int limit = 20) =>
            await _httpService.GetAsync<List<PriceBatch>>($"gestion/prix/lots?limit={limit}") ?? new();

        public Task<PriceUndoResult> UndoAsync(int batchId, bool onlyUntouched = false) =>
            _httpService.PostAsyncWithAuth<PriceUndoResult>("gestion/prix/annuler", new Dictionary<string, object>
            {
                ["batchId"] = batchId,
                ["onlyUntouched"] = onlyUntouched,
            });
    }
}
