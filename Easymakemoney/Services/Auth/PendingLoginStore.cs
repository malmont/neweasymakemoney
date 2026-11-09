namespace Easymakemoney.Services.Auth;

public class PendingLoginStore : IPendingLoginStore
{
    private static readonly TimeSpan ExpirationTime = TimeSpan.FromMinutes(10);
    private readonly object _lock = new();

    private string? _email;
    private string? _password;
    private DateTime? _storedAt;

    public void Set(string email, string password)
    {
        lock (_lock)
        {
            _email = email;
            _password = password;
            _storedAt = DateTime.UtcNow;
        }
    }

    public bool TryGet(out string email, out string password)
    {
        lock (_lock)
        {
            if (_email != null && _password != null && _storedAt.HasValue)
            {
                if (DateTime.UtcNow - _storedAt.Value < ExpirationTime)
                {
                    email = _email;
                    password = _password;
                    return true;
                }
            }

            // Expiré ou vide
            _email = null;
            _password = null;
            _storedAt = null;
            email = string.Empty;
            password = string.Empty;
            return false;
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _email = null;
            _password = null;
            _storedAt = null;
        }
    }
}

