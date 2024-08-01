using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Easymakemoney.Services
{
    public class HttpService : IHttpService
    {
        private readonly HttpClient _httpClient;
        private readonly IPreferenceService _preferenceService;

        public HttpService(HttpClient httpClient, IPreferenceService preferenceService)
        {
            _httpClient = httpClient;
            _preferenceService = preferenceService;
        }

        public async Task<T> GetAsync<T>(string url)
        {
            var token = _preferenceService.GetUserToken();
            if (string.IsNullOrEmpty(token))
            {
                throw new InvalidOperationException("Token is not available.");
            }

            // Ajouter le token JWT à l'en-tête de la requête
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

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

        public async Task<T> PostAsyncWithAuth<T>(string url, object data)
        {
            var token = _preferenceService.GetUserToken();
            if (string.IsNullOrEmpty(token))
            {
                throw new InvalidOperationException("Token is not available.");
            }

            // Ajouter le token JWT à l'en-tête de la requête
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

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

        public async Task<T> PostAsyncWithoutAuth<T>(string url, object data)
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
