namespace Easymakemoney.Services
{
    public class LoanService : ILoanService
    {
        private const string Route = "gestion/prets";

        private readonly IHttpService _httpService;

        public LoanService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<LoanList> ListAsync() =>
            await _httpService.GetAsync<LoanList>(Route) ?? new LoanList();

        public async Task<Loan> GetAsync(int id) =>
            await _httpService.GetAsync<Loan>($"{Route}/{id}") ?? new Loan { id = id };

        public async Task<LoanPreview> PreviewAsync(IReadOnlyDictionary<string, object?> body, CancellationToken cancellationToken = default) =>
            await _httpService.PostAsyncWithAuth<LoanPreview>($"{Route}/apercu", body, cancellationToken) ?? new LoanPreview();

        public Task<Loan?> CreateAsync(IReadOnlyDictionary<string, object?> body) =>
            _httpService.PostAsyncWithAuth<Loan?>(Route, body);

        public Task<Loan?> UpdateAsync(int id, IReadOnlyDictionary<string, object?> changes) =>
            _httpService.PatchAsyncWithAuth<Loan?>($"{Route}/{id}", changes);

        public async Task DeleteAsync(int id) =>
            await _httpService.DeleAsyncWithAuth<object>($"{Route}/{id}");

        public async Task AddPrepaymentAsync(int id, DateTime date, long amountCents, long? feesCents)
        {
            var body = new Dictionary<string, object> { ["date"] = TreasuryDates.Iso(date), ["amount"] = amountCents };
            if (feesCents is > 0)
                body["fees"] = feesCents.Value;
            await _httpService.PostAsyncWithAuth<object>($"{Route}/{id}/remboursements", body);
        }

        /// <summary>
        /// Le serveur rend la fiche recalculée (200) ; le client HTTP ne lit pas le corps d'un DELETE, la fiche est donc relue aussitôt.
        /// </summary>
        public async Task<Loan> DeletePrepaymentAsync(int id, int index)
        {
            await _httpService.DeleAsyncWithAuth<object>($"{Route}/{id}/remboursements/{index}");
            return await GetAsync(id);
        }
    }
}
