namespace Easymakemoney.Services.Auth;

public interface IPendingLoginStore
{
    void Set(string email, string password);
    bool TryGet(out string email, out string password);
    void Clear();
}

