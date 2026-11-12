using Easymakemoney.Services.Api;
using Newtonsoft.Json.Linq;

namespace Easymakemoney.Services
{
    public class DocumentReaderService : IDocumentReaderService
    {
        private const string BaseUrl = "gestion/documents";
        private readonly IHttpService _httpService;

        public DocumentReaderService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public Task<GestionDocument> UploadAsync(string type, IReadOnlyList<UploadFile> files) =>
            _httpService.PostFilesAsync<GestionDocument>(BaseUrl,
                new Dictionary<string, string> { ["type"] = type, ["locale"] = "fr" }, "files[]", files);

        public Task<GestionDocument> GetAsync(int id) =>
            _httpService.GetAsync<GestionDocument>($"{BaseUrl}/{id}");

        public async Task<List<GestionDocument>> ListAsync(string type, string status) =>
            await _httpService.GetAsync<List<GestionDocument>>($"{BaseUrl}?type={Uri.EscapeDataString(type)}&status={Uri.EscapeDataString(status)}&limit=20")
            ?? new List<GestionDocument>();

        public Task<GestionDocument> RereadAsync(int id) =>
            _httpService.PostAsyncWithAuth<GestionDocument>($"{BaseUrl}/{id}/relire", null!);

        public async Task SaveDraftAsync(int id, object draft) =>
            await _httpService.PatchAsyncWithAuth<object>($"{BaseUrl}/{id}/brouillon", draft);

        public async Task UploadLinePhotoAsync(int id, int n, UploadFile file) =>
            await _httpService.PostFilesAsync<object>($"{BaseUrl}/{id}/lignes/{n}/photo",
                new Dictionary<string, string>(), "file", new[] { file });

        public Task<JObject?> ConfirmAsync(int id, object body) =>
            _httpService.PostAsyncWithAuth<JObject?>($"{BaseUrl}/{id}/confirmer", body);

        public async Task<InvoicePreview> PreviewAsync(int id, object body) =>
            await _httpService.PostAsyncWithAuth<InvoicePreview>($"{BaseUrl}/{id}/apercu", body) ?? new InvoicePreview();

        public async Task DeleteAsync(int id) =>
            await _httpService.DeleAsyncWithAuth<object>($"{BaseUrl}/{id}");
    }
}
