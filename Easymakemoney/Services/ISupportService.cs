namespace Easymakemoney.Services
{
    public interface ISupportService
    {
        Task<SupportTicketList> GetTicketsAsync(string? status, string? assigned, string? query, int page);
        Task<SupportTicketDetail> GetTicketAsync(int id);
        Task<SupportTicket> CreateAsync(SupportTicketCreateRequest request);
        Task<SupportTicket> UpdateAsync(int id, IDictionary<string, object?> changes);
        Task AddMessageAsync(int id, string body, bool isInternal, bool fromCustomer);
    }

    /// <summary>Module Assistance, lot 1 du backend : routes /api/support (ROLE_ADMIN ou ROLE_SUPPORT).</summary>
    public class SupportService : ISupportService
    {
        public const int PageSize = 20;

        private readonly IHttpService _httpService;

        public SupportService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public static string ListUrl(string? status, string? assigned, string? query, int page)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(status))
                parts.Add($"status={Uri.EscapeDataString(status)}");
            if (!string.IsNullOrWhiteSpace(assigned))
                parts.Add($"assigned={Uri.EscapeDataString(assigned)}");
            if (!string.IsNullOrWhiteSpace(query))
                parts.Add($"q={Uri.EscapeDataString(query.Trim())}");
            parts.Add($"page={Math.Max(1, page)}");
            parts.Add($"pageSize={PageSize}");
            return "support/tickets?" + string.Join("&", parts);
        }

        public async Task<SupportTicketList> GetTicketsAsync(string? status, string? assigned, string? query, int page) =>
            await _httpService.GetAsync<SupportTicketList>(ListUrl(status, assigned, query, page)) ?? new SupportTicketList();

        public async Task<SupportTicketDetail> GetTicketAsync(int id) =>
            await _httpService.GetAsync<SupportTicketDetail>($"support/tickets/{id}") ?? new SupportTicketDetail();

        public async Task<SupportTicket> CreateAsync(SupportTicketCreateRequest request) =>
            await _httpService.PostAsyncWithAuth<SupportTicket>("support/tickets", request.ToBody()) ?? new SupportTicket();

        public async Task<SupportTicket> UpdateAsync(int id, IDictionary<string, object?> changes) =>
            await _httpService.PatchAsyncWithAuth<SupportTicket>($"support/tickets/{id}", changes) ?? new SupportTicket();

        public async Task AddMessageAsync(int id, string body, bool isInternal, bool fromCustomer)
        {
            var payload = new Dictionary<string, object> { ["body"] = body.Trim() };
            if (isInternal)
                payload["internal"] = true;
            if (fromCustomer)
                payload["fromCustomer"] = true;
            await _httpService.PostAsyncWithAuth<object>($"support/tickets/{id}/messages", payload);
        }
    }
}
