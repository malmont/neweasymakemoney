using System.Text;
using Easymakemoney.Models.Api;
using Easymakemoney.Models.Login;
using Newtonsoft.Json;

namespace Easymakemoney.Services
{
	public class LoginService : ILoginService
	{
		private readonly IHttpClientFactory _httpClientFactory;

		public LoginService(IHttpClientFactory httpClientFactory)
		{
			_httpClientFactory = httpClientFactory;
		}

		public async Task<(LoginResponse? Response, ApiError? Error)> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default)
		{
			var requestPayload = new LoginRequest
			{
				username = username,
				password = password,
				platform = "admin"
			};

			var json = JsonConvert.SerializeObject(requestPayload);
			using var request = new HttpRequestMessage(HttpMethod.Post, "login");
			request.Content = new StringContent(json, Encoding.UTF8, "application/json");

			var client = _httpClientFactory.CreateClient("auth");

			HttpResponseMessage response;
			string content;
			try
			{
				response = await client.SendAsync(request, cancellationToken);
				using (response)
				{
					content = await response.Content.ReadAsStringAsync(cancellationToken);
				}
			}
			catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
			{
				return (null, new ApiError { Code = "NO_CONNECTION", Message = "Pas de connexion." });
			}

			if (response.IsSuccessStatusCode)
			{
				try
				{
					var loginResponse = JsonConvert.DeserializeObject<LoginResponse>(content);
					return (loginResponse, null);
				}
				catch (Exception)
				{
					return (null, new ApiError { Code = "INVALID_RESPONSE", Message = "Réponse du serveur incomplète." });
				}
			}

			var apiError = ApiError.FromHttpResponse(content, response.StatusCode, response.Headers);
			return (null, apiError);
		}

		public async Task<(LoginResponse? Response, ApiError? Error)> VerifyOtpAsync(string username, string otp, CancellationToken cancellationToken = default)
		{
			var requestPayload = new OtpVerifyRequest
			{
				username = username,
				otp = otp,
				platform = "admin"
			};

			var json = JsonConvert.SerializeObject(requestPayload);
			using var request = new HttpRequestMessage(HttpMethod.Post, "otp-verify");
			request.Content = new StringContent(json, Encoding.UTF8, "application/json");

			var client = _httpClientFactory.CreateClient("auth");

			HttpResponseMessage response;
			string content;
			try
			{
				response = await client.SendAsync(request, cancellationToken);
				using (response)
				{
					content = await response.Content.ReadAsStringAsync(cancellationToken);
				}
			}
			catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
			{
				return (null, new ApiError { Code = "NO_CONNECTION", Message = "Pas de connexion." });
			}

			if (response.IsSuccessStatusCode)
			{
				try
				{
					var loginResponse = JsonConvert.DeserializeObject<LoginResponse>(content);
					return (loginResponse, null);
				}
				catch (Exception)
				{
					return (null, new ApiError { Code = "INVALID_RESPONSE", Message = "Réponse du serveur incomplète." });
				}
			}

			var apiError = ApiError.FromHttpResponse(content, response.StatusCode, response.Headers);
			return (null, apiError);
		}
	}
}
