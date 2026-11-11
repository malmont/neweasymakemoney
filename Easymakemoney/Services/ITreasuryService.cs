namespace Easymakemoney.Services
{
    public interface ITreasuryService
    {
        Task<TreasuryOverview> GetOverviewAsync(int days = 90);
        Task<List<TreasuryBalance>> GetBalancesAsync(int limit = 10);
        /// <summary>Enregistre le solde ; rend son rapprochement (null pour le solde d'ouverture).</summary>
        Task<Reconciliation?> AddBalanceAsync(DateTime date, long amountCents, string? note);
        Task<ReconciliationList> GetReconciliationsAsync(int limit = 10);
        Task DecideGapAsync(int balanceId, string decision, string? note);
        Task DeleteBalanceAsync(int balanceId);
        Task<List<TreasuryMovement>> GetMovementsAsync();
        Task AddMovementAsync(TreasuryMovementRequest request);
        Task DeleteMovementAsync(int movementId);
        Task UpdateSettingsAsync(long? threshold, bool? includeEstimatedSales);
        /// <summary>Frais fixes (lot FF1) ; all : aussi ceux à venir et terminés.</summary>
        Task<FixedCostList> GetFixedCostsAsync(bool all);
    }

    public sealed class TreasuryMovementRequest
    {
        public string label { get; init; } = string.Empty;
        public string category { get; init; } = "other";
        public string direction { get; init; } = "out";
        public long amount { get; init; }
        public string startDate { get; init; } = string.Empty;
        public string recurrence { get; init; } = "none";

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? endDate { get; init; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? notes { get; init; }

        /// <summary>Choix du gérant (FF1) ; absent = règle par défaut de la catégorie.</summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? fixedCost { get; init; }
    }
}
