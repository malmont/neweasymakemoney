using System.Threading.Tasks;

namespace Easymakemoney.Services
{
    public interface IHttpService
    {
        Task<T> GetAsync<T>(string url);
        Task<T> PostAsync<T>(string url, object data);
    }
}
