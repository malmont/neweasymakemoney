using Easymakemoney.Services.Api;
using Newtonsoft.Json.Linq;

namespace Easymakemoney.Services
{
    public interface IDocumentReaderService
    {
        Task<GestionDocument> UploadAsync(string type, IReadOnlyList<UploadFile> files);
        Task<GestionDocument> GetAsync(int id);
        Task<List<GestionDocument>> ListAsync(string type, string status);
        Task<GestionDocument> RereadAsync(int id);
        Task SaveDraftAsync(int id, object draft);
        Task UploadLinePhotoAsync(int id, int n, UploadFile file);
        Task<JObject?> ConfirmAsync(int id, object body);
        Task DeleteAsync(int id);
    }
}
