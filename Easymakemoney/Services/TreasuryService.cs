namespace Easymakemoney.Services
{
    public class TreasuryService : ITreasuryService
    {
        /// <summary>L'application est en français : libellés et remarques du backend en français.</summary>
        private const string Locale = "fr";

        private readonly IHttpService _httpService;

        public TreasuryService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public Task<TreasuryOverview> GetOverviewAsync(int days = 90) =>
            _httpService.GetAsync<TreasuryOverview>($"gestion/tresorerie?days={days}&locale={Locale}");

        public async Task<List<TreasuryBalance>> GetBalancesAsync(int limit = 10) =>
            await _httpService.GetAsync<List<TreasuryBalance>>($"gestion/tresorerie/soldes?limit={limit}") ?? new();

        public async Task<Reconciliation?> AddBalanceAsync(DateTime date, long amountCents, string? note)
        {
            var body = new Dictionary<string, object> { ["date"] = TreasuryDates.Iso(date), ["amount"] = amountCents };
            if (!string.IsNullOrWhiteSpace(note))
                body["note"] = note.Trim();
            var result = await _httpService.PostAsyncWithAuth<BalanceSaveResult>("gestion/tresorerie/soldes", body);
            return result?.reconciliation;
        }

        public async Task<ReconciliationList> GetReconciliationsAsync(int limit = 10) =>
            (await _httpService.GetAsync<ReconciliationList>($"gestion/tresorerie/rapprochements?limit={limit}") ?? new ReconciliationList()).WithCurrency();

        /// <summary>Décision du gérant sur un écart : unexplained, explained (avec une note) ou open (revenir sur la décision).</summary>
        public async Task DecideGapAsync(int balanceId, string decision, string? note)
        {
            var body = new Dictionary<string, object> { ["decision"] = decision };
            if (!string.IsNullOrWhiteSpace(note))
                body["note"] = note.Trim();
            await _httpService.PostAsyncWithAuth<object>($"gestion/tresorerie/soldes/{balanceId}/ecart", body);
        }

        public async Task DeleteBalanceAsync(int balanceId) =>
            await _httpService.DeleAsyncWithAuth<object>($"gestion/tresorerie/soldes/{balanceId}");

        public async Task<List<TreasuryMovement>> GetMovementsAsync() =>
            await _httpService.GetAsync<List<TreasuryMovement>>($"gestion/tresorerie/mouvements?locale={Locale}") ?? new();

        public async Task AddMovementAsync(TreasuryMovementRequest request) =>
            await _httpService.PostAsyncWithAuth<object>("gestion/tresorerie/mouvements", request);

        public async Task DeleteMovementAsync(int movementId) =>
            await _httpService.DeleAsyncWithAuth<object>($"gestion/tresorerie/mouvements/{movementId}");

        public async Task<FixedCostList> GetFixedCostsAsync(bool all) =>
            await _httpService.GetAsync<FixedCostList>($"gestion/frais-fixes?{(all ? "all=1&" : string.Empty)}locale={Locale}") ?? new FixedCostList();

        public async Task UpdateSettingsAsync(long? threshold, bool? includeEstimatedSales)
        {
            var body = new Dictionary<string, object>();
            if (threshold.HasValue)
                body["threshold"] = threshold.Value;
            if (includeEstimatedSales.HasValue)
                body["includeEstimatedSales"] = includeEstimatedSales.Value;
            await _httpService.PutAsyncWithAuth<object>("gestion/tresorerie/reglages", body);
        }
    }
}
