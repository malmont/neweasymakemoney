namespace Easymakemoney.Services.Auth;

public class AuthenticationMissingException : Exception
{
    public AuthenticationMissingException()
        : base("Aucun jeton d'authentification n'est disponible en session.")
    {
    }

    public AuthenticationMissingException(string message)
        : base(message)
    {
    }
}

