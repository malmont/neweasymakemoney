namespace Easymakemoney.Models.Tenant;

public class TenantContext
{
    private string? _name;

    public string Host { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    public string Name
    {
        get => string.IsNullOrWhiteSpace(_name) ? Code : _name;
        set => _name = value;
    }

    public string? Label { get; set; }
}

