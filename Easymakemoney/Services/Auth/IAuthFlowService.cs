namespace Easymakemoney.Services.Auth;

public interface IAuthFlowService
{
    Task LogoutAsync();
    Task<bool> PromptAndChangeSiteAsync();
}

