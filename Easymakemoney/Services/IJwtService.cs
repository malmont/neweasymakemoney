using Easymakemoney.Models.Session;

namespace Easymakemoney.Services;

public interface IJwtService
{
    bool TryParseAndValidate(string token, string expectedTenantCode, out UserInfo? user, out string? errorMessage);
    DateTime? GetExpirationUtc(string token);
}
