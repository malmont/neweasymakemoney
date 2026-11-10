namespace Easymakemoney.Services
{
    public interface IAdviceService
    {
        Task<AdviceList> GetAsync(int? periodId = null);
        Task DoneAsync(int adviceId, string? note = null);
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
            return (await _httpService.GetAsync<AdviceList>($"gestion/conseils?{period}status=new&locale={Locale}")).WithCurrency();
        }

        public async Task DoneAsync(int adviceId, string? note = null)
        {
            var body = new Dictionary<string, object>();
            if (!string.IsNullOrWhiteSpace(note))
                body["note"] = note.Trim();
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
