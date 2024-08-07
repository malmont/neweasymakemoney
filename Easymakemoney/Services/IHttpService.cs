using System.Threading.Tasks;

namespace Easymakemoney.Services
{
    public interface IHttpService
    {
        Task<T> GetAsync<T>(string url);
        Task<T> PostAsyncWithAuth<T>(string url, object data);
        Task<T> PostAsyncWithoutAuth<T>(string url, object data);
        Task<bool> DeleAsyncWithAuth<T>(string url);
    }
}
