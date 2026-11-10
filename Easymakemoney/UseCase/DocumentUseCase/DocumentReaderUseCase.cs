using Easymakemoney.Services.Api;

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
        public Task<GestionDocument> RereadAsync(int id) => _service.RereadAsync(id);
        public Task ConfirmAsync(int id, object body) => _service.ConfirmAsync(id, body);
        public Task DeleteAsync(int id) => _service.DeleteAsync(id);
    }
}
