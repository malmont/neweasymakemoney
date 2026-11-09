namespace Easymakemoney.Models.Login;

public class RefreshTokenRequest
{
    public required string refresh_token { get; set; }
    public string platform { get; set; } = "admin";
}

