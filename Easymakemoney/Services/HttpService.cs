// Service d'accès HTTP
using System.Text;
using Easymakemoney.Services.Auth;

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

        public async Task<T> PostAsyncWithAuth<T>(string url, object? data = null)
        {
            HttpContent? content = null;
            if (data != null)
            {
                var json = JsonConvert.SerializeObject(data);
                content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            var response = await _httpClient.PostAsync(url, content);
            var responseJson = await response.Content.ReadAsStringAsync();

            Debug.WriteLine($"Response Status Code: {response.StatusCode}");

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

        public async Task<bool> DeleAsyncWithAuth<T>(string url)
        {
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

            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };
            request.SetSkipAuth(true);

            var response = await _httpClient.SendAsync(request);
            var responseJson = await response.Content.ReadAsStringAsync();

            Debug.WriteLine($"Response Status Code: {response.StatusCode}");

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

        public async Task<T> PostMultipartAsyncWithAuth<T>(string url, ListProduct data, Stream fileStream, string fileName)
        {
            var content = new MultipartFormDataContent();

            content.Add(new StringContent(data.name), "name");
            content.Add(new StringContent(data.description), "description");
            content.Add(new StringContent(data.purchasePrice.ToString()), "purchasePrice");
            content.Add(new StringContent(data.coefficientMultiplier.ToString()), "coefficientMultiplier");

            if (data.category != null && data.category.Count > 0)
            {
                foreach (var category in data.category)
                {
                    content.Add(new StringContent(category.id.ToString()), "category_ids[]");
                }
            }

            if (data.style != null)
            {
                content.Add(new StringContent(data.style.id.ToString()), "style_id");
            }

            if (fileStream != null)
            {
                var streamContent = new StreamContent(fileStream);
                content.Add(streamContent, "image", fileName);
            }

            var response = await _httpClient.PostAsync(url, content);
            var responseJson = await response.Content.ReadAsStringAsync();

            Debug.WriteLine($"Response Status Code: {response.StatusCode}");

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
