using Newtonsoft.Json;

namespace Easymakemoney.Models
{
	public class LoginResponse
	{
		[JsonProperty("token")]
		public string? Token { get; set; }

		[JsonProperty("refresh_token")]
		public string? RefreshToken { get; set; }

		[JsonProperty("otp_required")]
		public bool OtpRequired { get; set; }

		[JsonProperty("message")]
		public string? Message { get; set; }
	}
}
