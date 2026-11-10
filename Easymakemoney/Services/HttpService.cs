// Service d'accès HTTP
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Easymakemoney.Models.Api;
using Easymakemoney.Services.Api;
using Easymakemoney.Services.Auth;
using Easymakemoney.Utilities;

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
            HttpResponseMessage response;
            try
            {
                response = await _httpClient.GetAsync(url);
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException or OperationCanceledException or TimeoutException)
            {
                throw ApiException.NoConnection(ex);
            }

            using (response)
            {
                return await HandleResponseAsync<T>(response);
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

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.PostAsync(url, content);
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException or OperationCanceledException or TimeoutException)
            {
                throw ApiException.NoConnection(ex);
            }

            using (response)
            {
                return await HandleResponseAsync<T>(response);
            }
        }

        public async Task<T> PatchAsyncWithAuth<T>(string url, object data)
        {
            HttpContent? content = null;
            if (data != null)
            {
                var json = JsonConvert.SerializeObject(data);
                content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.PatchAsync(url, content);
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException or OperationCanceledException or TimeoutException)
            {
                throw ApiException.NoConnection(ex);
            }

            using (response)
            {
                return await HandleResponseAsync<T>(response);
            }
        }

        public async Task<bool> DeleAsyncWithAuth<T>(string url)
        {
            HttpResponseMessage response;
            try
            {
                response = await _httpClient.DeleteAsync(url);
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException or OperationCanceledException or TimeoutException)
            {
                throw ApiException.NoConnection(ex);
            }

            using (response)
            {
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                var apiError = ApiError.FromHttpResponse(responseJson, response.StatusCode, response.Headers);
                throw new ApiException(apiError, response.StatusCode);
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

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request);
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException or OperationCanceledException or TimeoutException)
            {
                throw ApiException.NoConnection(ex);
            }

            using (response)
            {
                return await HandleResponseAsync<T>(response);
            }
        }

        public async Task<T> PostMultipartAsyncWithAuth<T>(string url, ListProduct data, Stream fileStream, string fileName)
        {
            var content = new MultipartFormDataContent();

            content.Add(new StringContent(data.name), "name");
            content.Add(new StringContent(data.description), "description");

            // Règle 4 bis : prix d'achat envoyé en entier centimes avec CultureInfo.InvariantCulture
            var purchasePriceCents = (long)Math.Round((data.purchasePrice ?? 0) * 100);
            content.Add(new StringContent(purchasePriceCents.ToString(CultureInfo.InvariantCulture)), "purchasePrice");
            content.Add(new StringContent((data.coefficientMultiplier ?? 0).ToString(CultureInfo.InvariantCulture)), "coefficientMultiplier");

            if (data.category != null && data.category.Count > 0)
            {
                foreach (var category in data.category)
                {
                    content.Add(new StringContent(category.id.ToString(CultureInfo.InvariantCulture)), "category_ids[]");
                }
            }

            if (data.style != null)
            {
                content.Add(new StringContent(data.style.id.ToString(CultureInfo.InvariantCulture)), "style_id");
            }

            if (fileStream != null)
            {
                var streamContent = new StreamContent(fileStream);
                content.Add(streamContent, "image", fileName);
            }

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.PostAsync(url, content);
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException or OperationCanceledException or TimeoutException)
            {
                throw ApiException.NoConnection(ex);
            }

            using (response)
            {
                return await HandleResponseAsync<T>(response);
            }
        }

        private static async Task<T> HandleResponseAsync<T>(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.NoContent)
                {
                    return default!;
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(responseJson))
                {
                    return default!;
                }

                UpdateSiteCurrencyFromJson(responseJson);

                var deserializeJson = JsonConvert.DeserializeObject<T>(responseJson);
                return deserializeJson ?? default!;
            }

            var errorJson = await response.Content.ReadAsStringAsync();
            var apiError = ApiError.FromHttpResponse(errorJson, response.StatusCode, response.Headers);
            throw new ApiException(apiError, response.StatusCode);
        }

        private static void UpdateSiteCurrencyFromJson(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (TryExtractCurrency(root, out var code))
                    {
                        SiteCurrency.SetCurrency(code);
                    }
                }
                else if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                {
                    var first = root[0];
                    if (first.ValueKind == JsonValueKind.Object && TryExtractCurrency(first, out var code))
                    {
                        SiteCurrency.SetCurrency(code);
                    }
                }
            }
            catch
            {
                // Ne bloque jamais la désérialisation
            }
        }

        private static bool TryExtractCurrency(JsonElement element, out string? currencyCode)
        {
            currencyCode = null;

            // Depuis G6, « currency » d'une commande peut être sa devise d'achat : seul « siteCurrency » fait alors foi.
            if (TryReadString(element, "siteCurrency", out currencyCode))
                return true;

            // Le « currency » d'un fournisseur est celui de ses commandes, pas celui du site.
            if (element.TryGetProperty("purchaseCurrency", out _))
                return false;

            return TryReadString(element, "currency", out currencyCode)
                || TryReadString(element, "Currency", out currencyCode);
        }

        private static bool TryReadString(JsonElement element, string name, out string? value)
        {
            value = null;
            if (!element.TryGetProperty(name, out var property) || property.ValueKind != JsonValueKind.String)
                return false;

            var text = property.GetString();
            if (string.IsNullOrWhiteSpace(text))
                return false;

            value = text.Trim();
            return true;
        }
    }
}
