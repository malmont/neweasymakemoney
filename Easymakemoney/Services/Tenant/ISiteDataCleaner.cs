namespace Easymakemoney.Services.Tenant;

public interface ISiteDataCleaner
{
    Task ClearAllAsync();
    Task<bool> PromptAndChangeSiteAsync();
}
