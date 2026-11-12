using Easymakemoney.Models;
using Easymakemoney.Services;
using Easymakemoney.Services.Api;
using Newtonsoft.Json.Linq;

namespace Easymakemoney.Tests.Fakes;

public class FakeDocumentReaderService : IDocumentReaderService
{
    public List<int> GetCalls { get; } = new();
    public List<(int Id, object Body)> ConfirmCalls { get; } = new();

    public GestionDocument DocumentResult { get; set; } = new();
    public JObject? ConfirmResult { get; set; }

    public Exception? GetException { get; set; }
    public Exception? ConfirmException { get; set; }

    public Task<GestionDocument> GetAsync(int id)
    {
        GetCalls.Add(id);
        if (GetException != null)
            throw GetException;
        return Task.FromResult(DocumentResult);
    }

    public Task<JObject?> ConfirmAsync(int id, object body)
    {
        ConfirmCalls.Add((id, body));
        if (ConfirmException != null)
            throw ConfirmException;
        return Task.FromResult(ConfirmResult);
    }

    public Task<GestionDocument> UploadAsync(string type, IReadOnlyList<UploadFile> files) => throw new NotImplementedException();
    public Task<List<GestionDocument>> ListAsync(string type, string status) => throw new NotImplementedException();
    public Task<GestionDocument> RereadAsync(int id) => throw new NotImplementedException();
    public Task SaveDraftAsync(int id, object draft) => throw new NotImplementedException();
    public Task UploadLinePhotoAsync(int id, int n, UploadFile file) => throw new NotImplementedException();
    public Task DeleteAsync(int id) => throw new NotImplementedException();
}

