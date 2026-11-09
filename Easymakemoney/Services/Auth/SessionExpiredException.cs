namespace Easymakemoney.Services.Auth;

public class SessionExpiredException : Exception
{
    public SessionExpiredException()
        : base("La session a expiré et le rafraîchissement a été refusé.")
    {
    }

    public SessionExpiredException(string message)
        : base(message)
    {
    }
}

