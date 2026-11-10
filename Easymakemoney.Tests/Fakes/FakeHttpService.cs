using Easymakemoney.Models;
using Easymakemoney.Services;

namespace Easymakemoney.Tests.Fakes;

public class FakeHttpService : IHttpService
{
    public List<(string Url, object Data)> PostedAuthData { get; } = new();
    public List<(string Url, object Data)> PostedWithoutAuthData { get; } = new();
    public List<string> RequestedUrls { get; } = new();
    public List<string> DeletedUrls { get; } = new();

    public object? ResultToReturn { get; set; }

    public Task<T> GetAsync<T>(string url)
    {
        RequestedUrls.Add(url);
        return Task.FromResult((T)(ResultToReturn ?? default(T)!));
    }

    public Task<T> PostFilesAsync<T>(string url, IDictionary<string, string> fields, string fileField, IEnumerable<Easymakemoney.Services.Api.UploadFile> files)
    {
        PostedAuthData.Add((url, fields));
        return Task.FromResult((T)(ResultToReturn ?? default(T)!));
    }

    public Task<T> PostAsyncWithAuth<T>(string url, object data)
    {
        PostedAuthData.Add((url, data));
        return Task.FromResult((T)(ResultToReturn ?? default(T)!));
    }

    public Task<T> PatchAsyncWithAuth<T>(string url, object data)
    {
        PostedAuthData.Add((url, data));
        return Task.FromResult((T)(ResultToReturn ?? default(T)!));
    }

    public Task<T> PostAsyncWithoutAuth<T>(string url, object data)
    {
        PostedWithoutAuthData.Add((url, data));
        return Task.FromResult((T)(ResultToReturn ?? default(T)!));
    }

    public Task<T> PostMultipartAsyncWithAuth<T>(string url, ListProduct data, Stream fileStream, string fileName)
    {
        PostedAuthData.Add((url, data));
        return Task.FromResult((T)(ResultToReturn ?? default(T)!));
    }

    public Task<bool> DeleAsyncWithAuth<T>(string url)
    {
        DeletedUrls.Add(url);
        return Task.FromResult(true);
    }
}

