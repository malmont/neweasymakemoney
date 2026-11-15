using System.Threading.Tasks;

namespace Easymakemoney.Services
{
    public interface IHttpService
    {
        Task<T> GetAsync<T>(string url);
        Task<T> PostAsyncWithAuth<T>(string url, object data);
        Task<T> PatchAsyncWithAuth<T>(string url, object data);
        Task<T> PutAsyncWithAuth<T>(string url, object data);
        Task<T> PostFilesAsync<T>(string url, IDictionary<string, string> fields, string fileField, IEnumerable<Easymakemoney.Services.Api.UploadFile> files);
        Task<T> PostAsyncWithoutAuth<T>(string url, object data);
        Task<T> PostMultipartAsyncWithAuth<T>(string url, ListProduct data, Stream fileStream, string fileName);
        Task<bool> DeleAsyncWithAuth<T>(string url);
        /// <summary>POST JSON dont la réponse est un fichier (PDF…) ; erreurs comme les autres appels.</summary>
        Task<Easymakemoney.Services.Api.DownloadedFile> PostForFileAsync(string url, object data);
        /// <summary>GET dont la réponse est un fichier (PDF…), avec le jeton ; erreurs comme les autres appels.</summary>
        Task<Easymakemoney.Services.Api.DownloadedFile> GetFileAsync(string url);
    }
}
