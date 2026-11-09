namespace Easymakemoney.Services.Tenant;

public class TenantMissingException : InvalidOperationException
{
    public TenantMissingException()
        : base("Aucun site actif configuré. Impossible d'envoyer la requête.")
    {
    }

    public TenantMissingException(string message)
        : base(message)
    {
    }

    public TenantMissingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

