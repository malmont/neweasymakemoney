namespace Easymakemoney.Services
{
    public class PeriodBudgetService : IPeriodBudgetService
    {
        private readonly IHttpService _httpService;

        public PeriodBudgetService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        /// <summary>L'application est en français : libellés des postes en français, quelle que soit la langue du téléphone.</summary>
        private const string Locale = "fr";

        public async Task<PeriodBudget> GetAsync(int periodId) =>
            (await _httpService.GetAsync<PeriodBudget>($"periodes/{periodId}/budget?locale={Locale}")).WithCurrency();

        /// <summary>Remplace le prévu d'un seul poste ; les autres gardent le leur (0 efface le prévu).</summary>
        public async Task<PeriodBudget> SavePlannedAsync(int periodId, string category, long plannedCents)
        {
            var body = new { lines = new[] { new { category, planned = plannedCents } } };
            return (await _httpService.PutAsyncWithAuth<PeriodBudget>($"periodes/{periodId}/budget?locale={Locale}", body)).WithCurrency();
        }

        public async Task<PeriodBudget> CopyAsync(int periodId, int? fromPeriodId = null)
        {
            object body = fromPeriodId.HasValue ? new { fromPeriodId = fromPeriodId.Value } : new { };
            return (await _httpService.PostAsyncWithAuth<PeriodBudget>($"periodes/{periodId}/budget/copier?locale={Locale}", body)).WithCurrency();
        }
    }
}
