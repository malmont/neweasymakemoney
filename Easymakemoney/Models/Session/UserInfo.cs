namespace Easymakemoney.Models.Session;

public class UserInfo
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public string TenantCode { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
}

