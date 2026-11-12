using Easymakemoney.Models;
using Easymakemoney.Services;
using Easymakemoney.Services.Api;
using Newtonsoft.Json.Linq;

namespace Easymakemoney.Tests.Fakes;

public class FakeDocumentReaderService : IDocumentReaderService
{
    public List<int> GetCalls { get; } = new();
    public List<(int Id, object Body)> ConfirmCalls { get; } = new();
    public List<(string Type, List<UploadFile> Files)> UploadCalls { get; } = new();
    public List<(string Type, string Status)> ListCalls { get; } = new();
    public List<int> RereadCalls { get; } = new();
    public List<int> DeleteCalls { get; } = new();
    public List<(int Id, object Draft)> SaveDraftCalls { get; } = new();
    public List<(int Id, int N, UploadFile File)> UploadLinePhotoCalls { get; } = new();

    public GestionDocument DocumentResult { get; set; } = new();
    public GestionDocument UploadResult { get; set; } = new();
    public GestionDocument RereadResult { get; set; } = new();
    public List<GestionDocument> ListResult { get; set; } = new();
    public JObject? ConfirmResult { get; set; }

    public Exception? GetException { get; set; }
    public Exception? ConfirmException { get; set; }
    public bool ConfirmExceptionOnlyInitial { get; set; }
    public Exception? UploadException { get; set; }
    public Exception? ListException { get; set; }
    public Exception? RereadException { get; set; }
    public Exception? DeleteException { get; set; }
    public Exception? SaveDraftException { get; set; }
    public Exception? UploadLinePhotoException { get; set; }

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
        if (ConfirmException != null && !(ConfirmExceptionOnlyInitial && ConfirmCalls.Count > 1))
            throw ConfirmException;
        return Task.FromResult(ConfirmResult);
    }

    public Task<GestionDocument> UploadAsync(string type, IReadOnlyList<UploadFile> files)
    {
        UploadCalls.Add((type, files.ToList()));
        if (UploadException != null)
            throw UploadException;
        return Task.FromResult(UploadResult);
    }

    public Task<List<GestionDocument>> ListAsync(string type, string status)
    {
        ListCalls.Add((type, status));
        if (ListException != null)
            throw ListException;
        return Task.FromResult(ListResult);
    }

    public Task<GestionDocument> RereadAsync(int id)
    {
        RereadCalls.Add(id);
        if (RereadException != null)
            throw RereadException;
        return Task.FromResult(RereadResult);
    }

    public Task DeleteAsync(int id)
    {
        DeleteCalls.Add(id);
        if (DeleteException != null)
            throw DeleteException;
        return Task.CompletedTask;
    }

    public Task SaveDraftAsync(int id, object draft)
    {
        SaveDraftCalls.Add((id, draft));
        if (SaveDraftException != null)
            throw SaveDraftException;
        return Task.CompletedTask;
    }

    public Task UploadLinePhotoAsync(int id, int n, UploadFile file)
    {
        UploadLinePhotoCalls.Add((id, n, file));
        if (UploadLinePhotoException != null)
            throw UploadLinePhotoException;
        return Task.CompletedTask;
    }
}
