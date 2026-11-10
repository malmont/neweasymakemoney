using Easymakemoney.Services.Api;
using Newtonsoft.Json.Linq;

namespace Easymakemoney.UseCase.DocumentUseCase
{
    public class DocumentReaderUseCase
    {
        private readonly IDocumentReaderService _service;

        public DocumentReaderUseCase(IDocumentReaderService service)
        {
            _service = service;
        }

        public Task<GestionDocument> UploadAsync(string type, IReadOnlyList<UploadFile> files) => _service.UploadAsync(type, files);
        public Task<GestionDocument> GetAsync(int id) => _service.GetAsync(id);
        public Task<List<GestionDocument>> ListAsync(string type, string status) => _service.ListAsync(type, status);
        public Task<GestionDocument> RereadAsync(int id) => _service.RereadAsync(id);
        public Task SaveDraftAsync(int id, object draft) => _service.SaveDraftAsync(id, draft);
        public Task UploadLinePhotoAsync(int id, int n, UploadFile file) => _service.UploadLinePhotoAsync(id, n, file);
        public Task<JObject?> ConfirmAsync(int id, object body) => _service.ConfirmAsync(id, body);
        public Task DeleteAsync(int id) => _service.DeleteAsync(id);
    }
}
