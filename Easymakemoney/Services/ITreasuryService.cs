namespace Easymakemoney.Services
{
    public interface ITreasuryService
    {
        Task<TreasuryOverview> GetOverviewAsync(int days = 90);
        Task<List<TreasuryBalance>> GetBalancesAsync(int limit = 10);
        Task AddBalanceAsync(DateTime date, long amountCents, string? note);
        Task DeleteBalanceAsync(int balanceId);
        Task<List<TreasuryMovement>> GetMovementsAsync();
        Task AddMovementAsync(TreasuryMovementRequest request);
        Task DeleteMovementAsync(int movementId);
        Task UpdateSettingsAsync(long? threshold, bool? includeEstimatedSales);
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
    }
}
