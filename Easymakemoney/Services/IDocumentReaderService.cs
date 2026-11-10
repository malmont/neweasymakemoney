using Easymakemoney.Services.Api;

namespace Easymakemoney.Services
{
    public interface IDocumentReaderService
    {
        Task<GestionDocument> UploadAsync(string type, IReadOnlyList<UploadFile> files);
        Task<GestionDocument> GetAsync(int id);
        Task<GestionDocument> RereadAsync(int id);
        Task ConfirmAsync(int id, object body);
        Task DeleteAsync(int id);
    }
}
