namespace Easymakemoney.Services
{
    public interface IAdviceService
    {
        Task<AdviceList> GetAsync(int? periodId = null);
        /// <param name="applied">Ce qui a été appliqué (pricePercent ou promotionPercent…), nul : repli du backend sur le conseil.</param>
        Task DoneAsync(int adviceId, string? note = null, IDictionary<string, object>? applied = null);
        Task DismissAsync(int adviceId);
        Task RefreshAsync(int? periodId = null);
    }

    public class AdviceService : IAdviceService
    {
        /// <summary>L'application est en français, comme la langue de rédaction du site.</summary>
        private const string Locale = "fr";

        private readonly IHttpService _httpService;

        public AdviceService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<AdviceList> GetAsync(int? periodId = null)
        {
            var period = periodId is > 0 ? $"periodId={periodId}&" : string.Empty;
            // status=all : les conseils faits reviennent avec leur mesure ; les écartés sont filtrés par l'écran.
            return (await _httpService.GetAsync<AdviceList>($"gestion/conseils?{period}status=all&locale={Locale}")).WithCurrency();
        }

        public async Task DoneAsync(int adviceId, string? note = null, IDictionary<string, object>? applied = null)
        {
            var body = new Dictionary<string, object>();
            if (!string.IsNullOrWhiteSpace(note))
                body["note"] = note.Trim();
            if (applied is { Count: > 0 })
                body["applied"] = applied;
            await _httpService.PostAsyncWithAuth<object>($"gestion/conseils/{adviceId}/fait", body);
        }

        public async Task DismissAsync(int adviceId) =>
            await _httpService.PostAsyncWithAuth<object>($"gestion/conseils/{adviceId}/ecarter", new Dictionary<string, object>());

        public async Task RefreshAsync(int? periodId = null)
        {
            var body = new Dictionary<string, object>();
            if (periodId is > 0)
                body["periodId"] = periodId.Value;
            await _httpService.PostAsyncWithAuth<object>("gestion/conseils/actualiser", body);
        }
    }
}
