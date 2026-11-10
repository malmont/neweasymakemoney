using Easymakemoney.Services.Api;

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

        public Task<GestionDocument> RereadAsync(int id) =>
            _httpService.PostAsyncWithAuth<GestionDocument>($"{BaseUrl}/{id}/relire", null!);

        public async Task ConfirmAsync(int id, object body) =>
            await _httpService.PostAsyncWithAuth<object>($"{BaseUrl}/{id}/confirmer", body);

        public async Task DeleteAsync(int id) =>
            await _httpService.DeleAsyncWithAuth<object>($"{BaseUrl}/{id}");
    }
}
