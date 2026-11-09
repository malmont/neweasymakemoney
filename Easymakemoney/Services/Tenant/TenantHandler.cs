namespace Easymakemoney.Services.Tenant;

public class TenantHandler : DelegatingHandler
{
    private const string TenantHeaderName = "X-Tenant-Host";
    private readonly ITenantStore _tenantStore;

    public TenantHandler(ITenantStore tenantStore)
    {
        _tenantStore = tenantStore;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Règle L4 : retirer tout X-Tenant-Host déjà présent sur la requête
        request.Headers.Remove(TenantHeaderName);

        // Exception 1 : l'activation part sans X-Tenant-Host
        if (request.Options.TryGetValue(TenantHandlerOptions.SkipTenantHost, out var skip) && skip)
        {
            return await base.SendAsync(request, cancellationToken);
        }

        // Exception 2 : check-mobile part avec l'hôte spécifié par l'appelant
        if (request.Options.TryGetValue(TenantHandlerOptions.CustomTenantHost, out var customHost))
        {
            // Règle L4 : si l'option est présente mais vide, refuser la requête immédiatement
            if (string.IsNullOrWhiteSpace(customHost))
            {
                throw new InvalidOperationException("L'hôte personnalisé fourni dans CustomTenantHost ne peut pas être vide.");
            }

            request.Headers.Add(TenantHeaderName, customHost);
            return await base.SendAsync(request, cancellationToken);
        }

        // Cas normal : contrôle du site enregistré via GetTenantAsync (L3)
        var tenant = await _tenantStore.GetTenantAsync(cancellationToken);
        if (tenant == null || string.IsNullOrWhiteSpace(tenant.Host))
        {
            throw new TenantMissingException();
        }

        request.Headers.Add(TenantHeaderName, tenant.Host);
        return await base.SendAsync(request, cancellationToken);
    }
}

