namespace Easymakemoney.Models.Login;

public class OtpVerifyRequest
{
    public required string username { get; set; }
    public required string otp { get; set; }
    public string platform { get; set; } = "admin";
}

