
using System.Text;

namespace Easymakemoney.Services
{
    public class HttpService : IHttpService
    {
        private readonly HttpClient _httpClient;

        public HttpService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<T> GetAsync<T>(string url)
        {
            var response = await _httpClient.GetAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var json = await response.Content.ReadAsStringAsync();
                var deserializeJson = JsonConvert.DeserializeObject<T>(json);
                return deserializeJson ?? default;
            }
            else
            {
                return default;
            }
        }

        public async Task<T> PostAsync<T>(string url, object data)
        {
            var json = JsonConvert.SerializeObject(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var deserializeJson = JsonConvert.DeserializeObject<T>(responseJson);
                return deserializeJson ?? default;
            }
            else
            {
                return default;
            }
        }
    }
}
