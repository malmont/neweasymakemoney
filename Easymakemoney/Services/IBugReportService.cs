using Easymakemoney.Services.Api;

namespace Easymakemoney.Services
{
    /// <summary>« Signaler un problème » et « Mes signalements » (lot INC-4) : /api/incidents/bug-reports.</summary>
    public interface IBugReportService
    {
        Task<BugReportResult> SendAsync(string description, string screen, string contextJson, UploadFile? screenshot);
        Task<BugReportList> GetMineAsync();
    }

    public class BugReportService : IBugReportService
    {
        public const string Route = "incidents/bug-reports";

        private readonly IHttpService _httpService;
        private readonly Incidents.IIncidentEnvironment _environment;

        public BugReportService(IHttpService httpService, Incidents.IIncidentEnvironment environment)
        {
            _httpService = httpService;
            _environment = environment;
        }

        public async Task<BugReportResult> SendAsync(string description, string screen, string contextJson, UploadFile? screenshot)
        {
            var fields = new Dictionary<string, string>
            {
                ["description"] = description.Trim(),
                ["app"] = BugReportRules.App,
                ["screen"] = screen,
                ["context"] = contextJson,
            };
            if (_environment.AppVersion is { Length: > 0 } version)
                fields["appVersion"] = version;
            var files = screenshot == null ? Array.Empty<UploadFile>() : new[] { screenshot };
            return await _httpService.PostFilesAsync<BugReportResult>(Route, fields, "screenshot", files) ?? new BugReportResult();
        }

        public async Task<BugReportList> GetMineAsync() =>
            await _httpService.GetAsync<BugReportList>(Route) ?? new BugReportList();
    }
}
