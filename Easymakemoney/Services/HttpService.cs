
using System.Text;

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

            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var json = JsonConvert.SerializeObject(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            var responseJson = await response.Content.ReadAsStringAsync();

            // Ajoutez des journaux pour vérifier la réponse du serveur
            Debug.WriteLine($"Response Status Code: {response.StatusCode}");
            Debug.WriteLine($"Response Content: {responseJson}");

            if (response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.Created)
            {
                var deserializeJson = JsonConvert.DeserializeObject<T>(responseJson);
                return deserializeJson ?? default;
            }
            else
            {
                return default;
            }
        }

        public async Task <bool> DeleAsyncWithAuth<T>(string url)
        {
            var token = _preferenceService.GetUserToken();

            if(string.IsNullOrEmpty(token)) {
                throw new InvalidOperationException("Token is not available.");
            }
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await _httpClient.DeleteAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
              return true;
            }
            else
            {
                return false;
            }


        }


        public async Task<T> PostAsyncWithoutAuth<T>(string url, object data)
        {
            var json = JsonConvert.SerializeObject(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            var responseJson = await response.Content.ReadAsStringAsync();

            // Ajoutez des journaux pour vérifier la réponse du serveur
            Debug.WriteLine($"Response Status Code: {response.StatusCode}");
            Debug.WriteLine($"Response Content: {responseJson}");

            if (response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.Created)
            {
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
